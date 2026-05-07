using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelMapper;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelPrintTool;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.Helper;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using OfficeOpenXml;
namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.PrintExcelMethod
{
    public class PrintLTAGExcel : IPrintExcelStrategy
    {
        private readonly LabDbContextSec _db;
        public PrintLTAGExcel(LabDbContextSec db)
        {
            _db = db;
        }
        public void PrintJsonData(ExcelSubmitDto Dto, ExcelPackage PackageWet, ExcelPackage PackagePhy)
        {

            string reportNumber = Dto.ReportNumber!;
            string buyer = Dto.Buyer!;
            string menu = Dto.MenuName!;
            string sampleDescription = Dto.SampleDescription!;
            var selectedRows = Dto.SelectedRows;


            List<CheckListDto> checkLists = new List<CheckListDto>();
            foreach (var row in selectedRows!) checkLists.Add(new CheckListDto().CreateDto(row, menu, sampleDescription));
            foreach (var dto in checkLists)
            {
                Console.WriteLine($"{dto.ItemName} -> {dto.Type}");
                var pkg = dto.Type == "Wet" ? PackageWet : PackagePhy;
                if (TemplateSheetNames.ContainsKey(dto.ItemName!) || TemplateSheetNamesNormal.ContainsKey(dto.ItemName!))
                    FillSheet(pkg, dto.ItemName!, dto,Dto, reportNumber);
            }
            PackageWet.Save();
            PackagePhy.Save();



        }


        private void FillSheet(
            ExcelPackage pkg,
            string itemName,
            CheckListDto dto,
            ExcelSubmitDto esDto,
            string reportNo)
        {
            var tplName = new TemplateSelector(TemplateSheetNames, TemplateSheetNamesNormal).GetTemplateName(itemName, dto.sampleDescription!);
            var template = pkg.Workbook.Worksheets[tplName];

            // 2) 计算需要几张 sheet
            var cellAddrs = CellMapper[itemName](itemName, dto.sampleDescription!);
            string[]? AfterWashCellAddrs = null;
            if (itemName == "DS to Washing" || itemName == "DS to Dry-clean" || itemName == "Appearance" || itemName == "Spirality/Skewing")
            {
                AfterWashCellAddrs = AfterWashCellMapper[itemName](itemName, dto.sampleDescription!);
            }



            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            var samples = dto.Sample!.Split(',').Select(s => s.Trim()).ToArray();
            if (itemName == "Air Permeability")
            {
                samples = dto.Sample!
                    .Split(',')
                    .Select(s => s.Trim())
                    .SelectMany(s => new[] { $"{s}", $"{s} × 5 Washes"})
                    .ToArray();
            }
            if (itemName == "Drying Rate of Fabrics")
            {
                samples = dto.Sample!
                    .Split(',')
                    .Select(s => s.Trim())
                    .SelectMany(s => new[] { $"{s}", $"{s} × 5 Washes", $"{s} × 10 Washes"})
                    .ToArray();
            }
            int[]? afterWashMap = null;
            if (itemName == "DS to Washing" || itemName == "DS to Dry-clean" || itemName == "Appearance" || itemName == "Spirality/Skewing")
            {
                var wp = _db.WetParameterAatccs
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                if (wp == null) wp = new WetParameterAatcc();
                string? afterWash = wp!.AfterWash;
                string? iron = wp!.Iron;
                string? ironMethod = wp!.IronMethod;
                samples = SampleNumCounter.GetSample(dto.Sample!, afterWash, iron, ironMethod);
                afterWashMap = SampleNumCounter.ExpandWashNumbers(samples!, afterWash!, iron);
            }
            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            int offset = 0;
            if (dto.sampleDescription!.Contains("Fabric"))
            {
                offset = OffsetRule.GetValueOrDefault(itemName, 0);
            }// 获取偏移量，默认为0
            int capacity = offset > 0 ? cellAddrs.Length / 2 : cellAddrs.Length; // 根据是否偏移计算每张 Sheet 的实际容量
            if (itemName == "Colour Fastness to Hot Pressing") { capacity = 3; }// 特例处理，实际容量为3
            if (itemName == "Appearance"||itemName== "Extension and Recovery") { capacity = 1; }
            if (itemName == "Air Permeability") { capacity = 2; }
            if ((itemName == "DS to Washing" ||itemName== "DS to Dry-clean")&& !dto.sampleDescription!.Contains("Fabric")) { capacity = 1; }
            if (itemName == "Absorbency") { capacity = 6; }
            int sheetCnt = (int)Math.Ceiling(samples!.Length / (double)capacity);


            List<ExcelWorksheet> sheets = new List<ExcelWorksheet>();
            for (int idx = 0; idx < sheetCnt; idx++)
            {
                ExcelWorksheet ws;
                if (idx == 0)
                {
                    ws = template; // 第一张用模板
                }
                else
                {
                    string newSheetName = $"{tplName} ({idx + 1})";
                    // 检查是否已经存在同名的 sheet
                    if (pkg.Workbook.Worksheets.Any(ws => ws.Name == newSheetName))
                    {
                        ws = pkg.Workbook.Worksheets[newSheetName];
                    }
                    else
                    {
                        ws = pkg.Workbook.Worksheets.Copy(tplName, newSheetName);
                    }
                }
                sheets.Add(ws);
            }
            //先复制后写入
            for (int idx = 0; idx < sheetCnt; idx++)
            {
                ExcelWorksheet ws = sheets[idx];
                /* 计算当前 sheet 要写的样本区间 */
                int start = idx * capacity;                         // 本 sheet 起始样本索引
                int end = Math.Min(start + capacity, samples.Length);
                int count = end - start;                            // 本 sheet 要写的样本数量
                if (count <= 0) continue;
                /* 取本 sheet 对应的那段样本 */
                string[] slice = samples.Skip(start).Take(count).ToArray();
                int[]? afmap = null;
                if (afterWashMap != null) afmap = afterWashMap.Skip(start).Take(count).ToArray();
                /* 把这段样本写进去 */
                WriteSamples(ws, slice, afmap, cellAddrs, AfterWashCellAddrs, itemName, dto.sampleDescription);

                // 5) 其余参数
                if (dto.Type == "Wet")
                {
                    var wp = _db.WetParameterAatccs
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                    var extraMap = WetExtraMap.GetValueOrDefault(itemName, (wp, dto, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>())(wp, dto, reportNo);

                    foreach (var kv in extraMap)
                    {
                        // 如果 wp 为 null，提供一个默认值或者跳过某些操作
                        if (wp == null)
                        {
                            var defaultWp = new WetParameterAatcc();
                            ws.Cells[kv.Key].Value = kv.Value(defaultWp, dto, reportNo);
                        }
                        else
                        {
                            ws.Cells[kv.Key].Value = kv.Value(wp, dto, reportNo);
                        }
                    }
                }
                else if (dto.Type == "Physics")
                {
                    var wp = _db.WetParameterAatccs 
                        .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                    var extraMap = PhyExtraMap.GetValueOrDefault(itemName, (wp,dto, esDto,ws,reportNo) => new Dictionary<string, Func<WetParameterAatcc,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>())(wp,dto, esDto, ws, reportNo);
                    foreach (var kv in extraMap)
                    {
                        ws.Cells[kv.Key].Value = kv.Value(wp,dto, esDto, ws, reportNo);
                    }
                }
            }
        }
        private static readonly Dictionary<string, string> TemplateSheetNamesNormal = new()
        {
            ["Appearance"] = "AppearanceAfterWashing",
            ["Appearance of Smoothness"] = "Smoothness Appearance",
            ["CF to Washing"] = "CFtoWRL",
            ["CF to Rubbing"] = "CFtoWRL",
            ["CF to Light"] = "CFtoWRL",
            ["CF to Perspiration"] = "CFtoPWD",
            ["CF to Water"] = "CFtoPWD",
            ["CF to Dry-clean"] = "CFtoPWD",
            ["CF to Chlorinated Water"] = "CFtoDye&Cl",
            ["Dye Transfer in Storage"] = "CFtoDye&Cl",
            ["CF to Chlorine Bleaching"] = "Bleach&SeaWater",
            ["CF to Non-Chlorine Bleaching"] = "Bleach&SeaWater",
            ["CF to Sea Water"] = "Bleach&SeaWater",
            ["CF to Saliva"] = "CFtoSaliva&Sweat",
            ["CF to Sweat"] = "CFtoSaliva&Sweat",
            ["Spirality/Skewing"] = "Spirality",

            ["Weight"] = "Weight",
            ["Fabric Construction(Weave)"]="Weave",
            ["Density"] = "Density",
            ["Yarn Count"] = "Yarn Count",
            ["Wicking"] = "Wicking",
            ["Twist"] = "Yarn Twist",
            ["Thickness"] = "Bow&Skew&Thickness",
            ["Pilling Resistance"] = "Pilling Resistance",
            ["Abrasion Resistance"]= "Abrasion Resistance",
            ["Drying Rate of Fabrics"] = "DryingRate",
            ["Bonding Strength"] = "Peel Bond",
            ["Zipper Strength"] = "Zipper Strength",
            ["Tensile Strength"] = "Tensile Strength",
            ["Tear Strength"] = "Tearing Strength",
            ["Torque & Tension"] = "Torque&Tension",
            ["Small Parts"] = "Torque&Tension",
            ["Air Permeability"] = "Air Permeability",
            ["Absorbency"] = "Absorbency",
            ["Resistance to Snapping of Snap Fasteners"] = "Snapping & Unsnapping",
            ["Resistance to Unsnapping of Snap Fasteners"] = "Snapping & Unsnapping",
            ["Water Repellency-Spray Test"] = "Water Repellency",
        };
        private static readonly Dictionary<string, Dictionary<string[], string>> TemplateSheetNames = new()
        {
            ["DS to Washing"] = new Dictionary<string[], string>
            {
                { new[]{ "Fabric" },"DStoWashing-F" },
                { new[]{ "Garment" }, "DStoWashing-G" },
                { new[]{ "Socks" }, "DStoWashing-Acc" },
                { new[]{ "Gloves" }, "DStoWashing-Acc" },
                { new[]{ "Cap" },  "DStoWashing-Acc" },
            },
            ["DS to Dry-clean"] = new Dictionary<string[], string>
            {
                { new[]{ "Fabric" },"DStoDryclean-F" },
                { new[]{ "Garment" },"DStoDryclean-G" },
                { new[]{ "Socks" }, "DStoDryclean-Acc" },
                { new[]{ "Gloves" }, "DStoDryclean-Acc" },
                { new[]{ "Cap" },  "DStoDryclean-Acc" },
            },
            ["Seam Strength"] = new Dictionary<string[], string> 
            {
                { new[]{ "Fabric" },"Seam Slippage&Strength" },
                { new[]{ "Garment" ,"Knit"},"Seam Bursting-G" },
                { new[]{ "Garment" },"Seam Slippage&Strength-G" },
            },
            ["Seam Slippage"] = new Dictionary<string[], string>
            {
                { new[]{ "Fabric" },"Seam Slippage&Strength" },
                { new[]{ "Garment" },"Seam Slippage&Strength-G" },
            },
            ["Bursting Strength"] = new Dictionary<string[], string>
            {
                { new[]{ "Fabric" },"Bursting Strength" },
                { new[]{ "Garment" },"Seam Bursting-G" },
            },
            ["Extension and Recovery"] = new Dictionary<string[], string>
            {
                { new[]{ "Woven" },"ASTM D3107" },
                { new[]{ "Knit" },"ASTM D2594" },
            }
        };


        // 取映射地址的函数
        private static readonly Dictionary<string, Func<string, string, string[]>> CellMapper = new()
        {
            ["Appearance"] = (n, m) => ExcelLTAGMapper.MapAppearance(),
            ["Appearance of Smoothness"] = (n, m) => ExcelLTAGMapper.MapAppearance(),
            ["CF to Washing"] = (n, m) => ExcelLTAGMapper.MapCFtoWashing(),
            ["CF to Rubbing"] = (n, m) => ExcelLTAGMapper.MapCFtoRubbing(),
            ["CF to Light"] = (n, m) => ExcelLTAGMapper.MapCFtoLight(),
            ["CF to Perspiration"] = (n, m) => ExcelLTAGMapper.MapCFtoPerspiration(),
            ["CF to Water"] = (n, m) => ExcelLTAGMapper.MapCFtoWater(),
            ["CF to Dry-clean"] = (n, m) => ExcelLTAGMapper.MapCFtoDC(),
            ["CF to Chlorinated Water"] = (n, m) => ExcelLTAGMapper.MapCl(),
            ["Dye Transfer in Storage"] = (n, m) => ExcelLTAGMapper.MapDyeTransfer(),
            ["CF to Chlorine Bleaching"] = (n, m) => ExcelLTAGMapper.MapBleach(n),
            ["CF to Non-Chlorine Bleaching"] = (n, m) => ExcelLTAGMapper.MapBleach(n),
            ["CF to Sea Water"] = (n, m) => ExcelLTAGMapper.MapCFtoSeaWater(),
            ["CF to Saliva"] = (n, m) => ExcelLTAGMapper.MapCFtoSalivaSweat(),
            ["CF to Sweat"] = (n, m) => ExcelLTAGMapper.MapCFtoSalivaSweat(),
            ["Spirality/Skewing"] = (n, m) => ExcelLTAGMapper.MapSpirality(),
            ["DS to Dry-clean"]= (n, m) => ExcelLTAGMapper.MapDStoWashing(m),
            ["DS to Washing"] = (n, m) => ExcelLTAGMapper.MapDStoWashing(m),

            ["Weight"] = (n, m) => ExcelLTAGMapper.MapWeight(),
            ["Fabric Construction(Weave)"] = (n, m) => ExcelLTAGMapper.MapWeave(),
            ["Density"] = (n, m) => ExcelLTAGMapper.MapDensity(),
            ["Yarn Count"]= (n, m) => ExcelLTAGMapper.MapYarn(),
            ["Wicking"] = (n, m) => ExcelLTAGMapper.MapWicking(),
            ["Twist"] = (n, m) => ExcelLTAGMapper.MapTwist(),
            ["Thickness"] = (n, m) => ExcelLTAGMapper.MapThickness(),
            ["Pilling Resistance"] = (n, m) => ExcelLTAGMapper.MapPilling(),
            ["Abrasion Resistance"] = (n, m) => ExcelLTAGMapper.MapAbrasion(),
            ["Drying Rate of Fabrics"] = (n, m) => ExcelLTAGMapper.MapDryRate(),
            ["Bonding Strength"] = (n, m) => ExcelLTAGMapper.MapBond(),
            ["Zipper Strength"] = (n, m) => ExcelLTAGMapper.MapZipperStrength(),
            ["Tensile Strength"] = (n, m) => ExcelLTAGMapper.MapTensile(),
            ["Tear Strength"] = (n, m) => ExcelLTAGMapper.MapTear(),
            ["Torque & Tension"] = (n, m) => ExcelLTAGMapper.TorqueTension(),
            ["Small Parts"] = (n, m) => ExcelLTAGMapper.TorqueTension(),
            ["Air Permeability"] = (n, m) => ExcelLTAGMapper.MapAirPermeability(),
            ["Absorbency"] = (n, m) => ExcelLTAGMapper.MapAbsorbency(),
            ["Resistance to Snapping of Snap Fasteners"] = (n, m) => ExcelLTAGMapper.MapUnSnapping(),
            ["Resistance to Unsnapping of Snap Fasteners"] = (n, m) => ExcelLTAGMapper.MapUnSnapping(),
            ["Water Repellency-Spray Test"] = (n, m) => ExcelLTAGMapper.MapRepellency(),
            ["Extension and Recovery"] = (n, m) => ExcelLTAGMapper.MapExtensionAndRecovery(),
            ["Bursting Strength"] = (n, m) => ExcelLTAGMapper.MapBursting(m),
            ["Seam Slippage"]  = (n, m) => ExcelLTAGMapper.MapSeamSlippage(n,m),      
            ["Seam Strength"]= (n, m) => ExcelLTAGMapper.MapSeamStrength(n,m),
        };

        //取洗涤遍数映射地址的函数
        private static readonly Dictionary<string, Func<string, string, string[]>> AfterWashCellMapper = new()
        {
            ["DS to Washing"] = (_, m) => ExcelLTAGMapper.DStoWashingAf(m),
            ["DS to Dry-clean"] = (_, m) => ExcelLTAGMapper.DStoDCAf(m),
            ["Spirality/Skewing"] = (_, _) => ExcelLTAGMapper.SpiralityAf(),
            ["Appearance"] = (_, _) => ExcelLTAGMapper.AppearanceAf(),
        };


        private static readonly Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>>> WetExtraMap = new()
        {
            ["DS to Washing"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>();
                map["A3"] = (w, dto, reportNo) => dto.Standard!;
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    if (w.WashingProcedure!.Contains("Machine"))
                    {
                        map["N1"] = (w, dto, reportNo) => reportNo;
                        map["A5"] = (w, dto, reportNo) => w.Cycle + " Cycle";
                        map["V4"] = (w, dto, reportNo) => w.Temperature!;
                        map["E4"] = (w, dto, reportNo) => w.Program!;
                        map["AF4"] = (w, dto, reportNo) => w.Detergent!;
                        map["N5"] = (w, dto, reportNo) => w.DryProcedure!;
                        map["K4"] = (w, dto, reportNo) => w.DryCondition!;
                        map["W5"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!;
                        map["A8"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;

                    }
                    else if (w.WashingProcedure.Contains("Hand"))
                    {
                        map["N1"] = (w, dto, reportNo) => reportNo;
                        map["G7"] = (w, dto, reportNo) => w.Temperature!;
                        map["L7"] = (w, dto, reportNo) => w.DryProcedure!;
                        map["A8"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                    }
                }
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    if (w.WashingProcedure!.Contains("Machine"))
                    {
                        map["O1"] = (w, dto, reportNo) => reportNo;
                        map["A5"] = (w, dto, reportNo) => w.Cycle + " Cycle";
                        map["V4"] = (w, dto, reportNo) => w.Temperature!;
                        map["E4"] = (w, dto, reportNo) => w.Program!;
                        map["AE4"] = (w, dto, reportNo) => w.Detergent!;
                        map["N5"] = (w, dto, reportNo) => w.DryProcedure!;
                        map["K4"] = (w, dto, reportNo) => w.DryCondition!;
                        map["W5"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!;
                        map["A8"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;

                    }
                    else if (w.WashingProcedure.Contains("Hand"))
                    {
                        map["O1"] = (w, dto, reportNo) => reportNo;
                        map["H7"] = (w, dto, reportNo) => w.Temperature!;
                        map["N7"] = (w, dto, reportNo) => w.DryProcedure!;
                        map["A8"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                    }
                }
                else if (dto.sampleDescription!.Contains("Cap")|| dto.sampleDescription!.Contains("g")|| dto.sampleDescription!.Contains("Cap"))
                {
                    if (w.WashingProcedure!.Contains("Machine"))
                    {
                        map["P1"] = (w, dto, reportNo) => reportNo;
                        map["A5"] = (w, dto, reportNo) => w.Cycle + " Cycle";
                        map["V4"] = (w, dto, reportNo) => w.Temperature!;
                        map["E4"] = (w, dto, reportNo) => w.Program!;
                        map["AE4"] = (w, dto, reportNo) => w.Detergent!;
                        map["N5"] = (w, dto, reportNo) => w.DryProcedure!;
                        map["K4"] = (w, dto, reportNo) => w.DryCondition!;
                        map["W5"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!;
                        map["A8"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;

                    }
                    else if (w.WashingProcedure.Contains("Hand"))
                    {
                        map["P1"] = (w, dto, reportNo) => reportNo;
                        map["H7"] = (w, dto, reportNo) => w.Temperature!;
                        map["N7"] = (w, dto, reportNo) => w.DryProcedure!;
                        map["A8"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                    }
                }
                return map;
            },
            ["DS to Dry-clean"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>();
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["M1"] = (w, dto, reportNo) => reportNo;
                    map["A3"] = (w, dto, reportNo) => dto.Standard!;
                    map["F4"] = (w, dto, reportNo) => w.Sensitive == "Y" ? "Sensitive" : "Normal";
                }
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["O1"] = (w, dto, reportNo) => reportNo;
                    map["A3"] = (w, dto, reportNo) => dto.Standard!;
                    map["G4"] = (w, dto, reportNo) => w.Sensitive == "Y" ? "Sensitive" : "Normal";
                }
                else if (dto.sampleDescription!.Contains("Cap") || dto.sampleDescription!.Contains("Gloves") || dto.sampleDescription!.Contains("Socks")) 
                {
                    map["P1"] = (w, dto, reportNo) => reportNo;
                    map["A3"] = (w, dto, reportNo) => dto.Standard!;
                    map["G4"] = (w, dto, reportNo) => w.Sensitive == "Y" ? "Sensitive" : "Normal";
                }
                    return map;
            },
            ["Spirality/Skewing"] = (w, dto, reportNo) => 
            {
                var map = new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>();
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["P1"] = (w, dto, reportNo) => reportNo;
                    if (w.WashingProcedure!.Contains("Machine"))
                    {
                        map["A33"] = (w, dto, reportNo) => w.Cycle + " Cycle";
                        map["V32"] = (w, dto, reportNo) => w.Temperature!;
                        map["E32"] = (w, dto, reportNo) => w.Program!;
                        map["AE32"] = (w, dto, reportNo) => w.Detergent!;
                        map["M33"] = (w, dto, reportNo) => w.DryProcedure!;
                        map["J32"] = (w, dto, reportNo) => w.DryCondition!;
                        map["V33"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!;
                        map["A37"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;

                    }
                    else if (w.WashingProcedure.Contains("Hand"))
                    {
                        map["H36"] = (w, dto, reportNo) => w.Temperature!;
                        map["M36"] = (w, dto, reportNo) => w.DryProcedure!;
                        map["A37"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                    }
                }
                else if (string.IsNullOrEmpty(w.DryCleanProcedure)==false)
                {
                    map["P1"] = (w, dto, reportNo) => reportNo;
                    map["H32"] = (w, dto, reportNo) => w.Sensitive == "Y" ? "Sensitive" : "Normal"; ;
                }
                return map;
            },
            ["Appearance"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>();
                if (string.IsNullOrEmpty(w.WashingProcedure)==false)
                {
                    map["BC1"] = (w, dto, reportNo) => reportNo;
                    map["AR6"] = (w, dto, reportNo) => "√";
                    map["AU6"] = (w, dto, reportNo) => dto.sampleDescription!.Contains("Fabric")?"AATCC TM135-2018t":"AATCC TM150-2018t"!;
                    map["BI7"] = (w, dto, reportNo) => w.Cycle + " Cycle";
                    map["BY6"] = (w, dto, reportNo) => w.Temperature!;
                    map["BJ6"] = (w, dto, reportNo) => w.Program!;
                    map["AY7"]= (w, dto, reportNo) => w.Detergent!;
                    map["BP7"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["BO6"] = (w, dto, reportNo) => w.DryCondition!;
                    map["BW7"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!;
                    map["AR10"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                    map["BN23"] = (w, dto, reportNo) => w.IronMethod!;

                }
                else if (string.IsNullOrEmpty(w.DryCleanProcedure) == false)
                {
                    map["BC1"] = (w, dto, reportNo) => reportNo;
                    map["AR9"] = (w, dto, reportNo) => "√";
                    map["BL9"] = (w, dto, reportNo) => w.Sensitive == "Y" ? "Sensitive" : "Normal";
                    map["BN23"] = (w, dto, reportNo) => w.IronMethod!;
                    map["AR10"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                return map;
            },
            ["Appearance of Smoothness"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>();
                map["BC1"] = (w, dto, reportNo) => reportNo;
                map["AR4"] = (w, dto, reportNo) => dto.Standard!;
                map["AT6"] = (w, dto, reportNo) => "1";
                return map;
            },
            ["CF to Washing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>
            {
                ["E1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["B4"] = (w, dto, reportNo) => w!.Program!,
                ["F4"] = (w, dto, reportNo) => w!.Temperature!,
                ["H5"] = (w, dto, reportNo) => w!.SteelBallNum.ToString()!,
                ["J5"] = (w, dto, reportNo) => w!.SteelBallType!,
                ["I4"] = (w, dto, reportNo) => w!.Detergent!,
            },
            ["CF to Rubbing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>
            {
                ["E1"] = (w, dto, reportNo) => reportNo,
                ["A19"] = (w, dto, reportNo) => dto.Standard!

            },
            ["CF to Light"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>
            {
                ["E1"] = (w, dto, reportNo) => reportNo,
                ["A26"] = (w, dto, reportNo) => dto.Standard!,
                ["B30"] = (w, dto, reportNo) => "20"
            },
            ["CF to Perspiration"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!
            },
            ["CF to Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A25"] = (w, dto, reportNo) => dto.Standard!
            },
            ["CF to Dry-clean"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A37"] = (w, dto, reportNo) => dto.Standard!
            },
            ["Spirality/Skewing"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>();
                map["P1"] = (w, dto, reportNo) => reportNo;
                map["A3"] = (w, dto, reportNo) => dto.sampleDescription!.Contains("Garment") == true ? "AATCC TM 179-2023, Method 2, Option 3" : "AATCC TM 179-2023, Method 1, Option 1";
                if (w.WashingProcedure!.Contains("Machine"))
                {
                    map["O31"] = (w, dto, reportNo) => "AATCC TM 179-2023";
                    map["E32"] = (w, dto, reportNo) => w.Program!;
                    map["J32"] = (w, dto, reportNo) => w.DryCondition!;
                    map["V32"] = (w, dto, reportNo) => w.Temperature!;
                    map["AE32"] = (w, dto, reportNo) => w.Detergent!;
                    map["A33"] = (w, dto, reportNo) => w.Cycle!;
                    map["M33"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["V33"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!;
                }
                else if (w.WashingProcedure.Contains("Hand"))
                {
                    map["O35"] = (w, dto, reportNo) => "AATCC TM 179-2023";
                    map["G36"] = (w, dto, reportNo) => w.Temperature!;
                    map["K36"] = (w, dto, reportNo) => w.DryProcedure!;
                }
                return map;
            },
            ["CF to Chlorinated Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A17"] = (w, dto, reportNo) => dto.Standard!,
                ["B18"] = (w, dto, reportNo) => "20"
            },
            ["Dye Transfer in Storage"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!
            },
            ["CF to Chlorine BleachingCF to Chlorine Bleaching"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) =>reportNo,
                ["L4"] = (w, dto, reportNo) => dto.Parameter!.Contains("N/A") ? "N/A" : "-",
            },
            ["CF to Non-Chlorine Bleaching"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["L4"] = (w, dto, reportNo) =>dto.Parameter!.Contains("N/A") ? "N/A" : "-",
            },
            ["CF to Sea Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>> 
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A28"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["CF to Saliva"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["G3"] = (w, dto, reportNo) => "√"
            },
            ["CF to Sweat"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["J3"] = (w, dto, reportNo) => "√"
            },
        };

        private static readonly Dictionary<string, Func<WetParameterAatcc,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, Dictionary<string, Func<WetParameterAatcc,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>>> PhyExtraMap = new()
        {
            ["Weight"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["J1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Yarn Count"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Twist"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Density"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Fabric Construction(Weave)"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
            },
            ["Thickness"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A33"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Wicking"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["J1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Pilling Resistance"] = (wp, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                map["I2"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                map["H3"] = (wp, dto, esDto, ws, reportNo) => "30 min";
                map["A4"] = (wp, dto, esDto, ws, reportNo) => "√";
                if (dto.sampleDescription!.Contains("Anti"))
                {
                    if (wp.WashingProcedure!.Contains("Machine"))
                    {
                        map["P4"] = (wp, dto, esDto, ws, reportNo) => "√";
                        map["T4"] = (wp, dto, esDto, ws, reportNo) => "3";
                        map["A26"] = (wp, dto, esDto, ws, reportNo) => "√";
                        map["S27"] = (wp, dto, esDto, ws, reportNo) => wp.Cycle + " Cycle";
                        map["AH26"] = (wp, dto, esDto, ws, reportNo) => wp.Temperature!;
                        map["R26"] = (wp, dto, esDto, ws, reportNo) => wp.Program!;
                        map["I27"] = (wp, dto, esDto, ws, reportNo) => wp.Detergent!;
                        map["Z27"] = (wp, dto, esDto, ws, reportNo) => wp.DryProcedure!;
                        map["W26"] = (wp, dto, esDto, ws, reportNo) => wp.DryCondition!;
                        map["AG27"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.Iron!) == true ? "/ Iron" : wp.IronMethod!;
                        map["A30"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                    }
                    else if (wp.WashingProcedure!.Contains("Hand"))
                    {
                        map["A29"] = (wp, dto, esDto, ws, reportNo) => "√";
                        map["Z29"] = (wp, dto, esDto, ws, reportNo) => wp.Temperature!;
                        map["AF29"] = (wp, dto, esDto, ws, reportNo) => wp.DryProcedure!;
                        map["A30"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                    }
                }
                return map;
            },
            ["Abrasion Resistance"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
                ["C5"] = (wp, dto, esDto, ws, reportNo) => "9 KPa",
                ["I5"] = (wp, dto, esDto, ws, reportNo) => dto.Parameter!.Contains("15000") ? "15000"
                : dto.Parameter!.Contains("25000") ? "25000"
                : dto.Parameter!.Contains("35000") ? "35000"
                : "15000",
            },
            ["Drying Rate of Fabrics"] = (wp, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["J1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                map["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                map["Q4"] = (wp, dto, esDto, ws, reportNo) => "30";
                if (wp.WashingProcedure!.Contains("Machine"))
                {
                    map["A44"] = (wp, dto, esDto, ws, reportNo) => "√";
                    map["N45"] = (wp, dto, esDto, ws, reportNo) => wp.Cycle + " Cycle";
                    map["Z44"] = (wp, dto, esDto, ws, reportNo) => wp.Temperature!;
                    map["M44"] = (wp, dto, esDto, ws, reportNo) => wp.Program!;
                    map["G45"] = (wp, dto, esDto, ws, reportNo) => wp.Detergent!;
                    map["S45"] = (wp, dto, esDto, ws, reportNo) => wp.DryProcedure!;
                    map["R44"] = (wp, dto, esDto, ws, reportNo) => wp.DryCondition!;
                    map["Z45"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.Iron!) == true ? "/ Iron" : wp.IronMethod!;
                    map["A48"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                }
                else if (wp.WashingProcedure!.Contains("Hand"))
                {
                    map["A47"] = (wp, dto, esDto, ws, reportNo) => "√";
                    map["S47"] = (wp, dto, esDto, ws, reportNo) => wp.Temperature!;
                    map["V47"] = (wp, dto, esDto, ws, reportNo) => wp.DryProcedure!;
                    map["A48"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                }
                return map;
            },
            ["Extension and Recovery"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
                ["AR1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["AO3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Bursting Strength"] = (wp,dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (wp,dto, esDto, ws, reportNo) => esDto.ReportNumber!;
                if (dto.sampleDescription!.Contains("Fabric")) map["I3"] = (wp,dto, esDto, ws, reportNo) => dto.Standard!;
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["J3"] = (wp,dto, esDto, ws, reportNo) => dto.Standard!;
                    var sample = ws.Cells["D3"].Value?.ToString();

                    var cellOrder = new List<string> { "A7", "A8", "A9", "A10", "A11", "A12", "A13", "A14", "A15", "A16", "A17", "A18" };
                    var reasonCellOrder = cellOrder.Select(c => "Y" + c.Substring(1)).ToList();
                    if (sample.ToLower().Contains("shell"))
                    {
                        reasonCellOrder = cellOrder.Select(c => "J" + c.Substring(1)).ToList();
                    }
                    if (sample.ToLower().Contains("lining"))
                    {
                        reasonCellOrder = cellOrder.Select(c => "Y" + c.Substring(1)).ToList();
                    }
                    var descMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Side"] = "Side Seam",
                        ["Sleeve"] = "Sleeve Seam",
                        ["Armhole"] = "Armhole Seam",
                        ["Shoulder"] = "Shoulder Seam",
                        ["Armprit"] = "Armprit Seam",
                        ["Front Panel"] = "Front Panel Seam",
                        ["Back Panel"] = "Back Panel Seam",
                        ["OutSide"] = "Out-Side Seam",
                        ["InSide"] = "In-Side Seam",
                        ["Back Rise"] = "Back Rise Seam",
                        ["Front Crotch"] = "Front Crotch Seam",
                        ["Cross"] = "Cross Seam",
                    };

                    var seamInfos = esDto.SeamParameter
                                      ?.FirstOrDefault(s => s.Sample == sample)   // 找到当前行样本
                                      ?.LocationInfos
                                      ?.Where(x => !string.IsNullOrWhiteSpace(x.Location)) // 去掉空Location
                                      .ToList();
                    if (seamInfos?.Count > 0)
                    {
                        for (int i = 0; i < seamInfos.Count && i < cellOrder.Count; i++)
                        {
                            string location = seamInfos[i].Location!.Trim();
                            if (descMap.TryGetValue(location, out var desc))
                            {
                                string cell = cellOrder[i];
                                map[cell] = (wp, dto, esDto, ws, reportNo) => desc;   // 填入对应描述
                            }
                        }
                    }

                    for (int i = 0; i < seamInfos!.Count && i < cellOrder.Count; i++)
                    {
                        var info = seamInfos[i];
                        string location = info.Location!.Trim();

                        // 1. 填描述（原逻辑）
                        if (descMap.TryGetValue(location, out var desc))
                        {
                            string cell = cellOrder[i];
                            map[cell] = (wp, dto, esDto, ws, reportNo) => desc;
                        }

                        // 2. 当 IsNA == false 时，把 Reason 写到同行 J 列
                        if (info.IsNA == true && !string.IsNullOrWhiteSpace(info.Reason))
                        {
                            string reasonCell = reasonCellOrder[i];
                            string reason = "N/A；" + info.Reason;          // 捕获局部变量
                            map[reasonCell] = (wp, dto, esDto, ws, reportNo) => reason;
                        }
                    }
                }
                return map;
            },
            ["Seam Slippage"] = (wp,dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (wp, dto, esDto, ws, reportNo) => esDto.ReportNumber!;
                if (dto.sampleDescription!.Contains("Fabric")) map["A3"] = (wp,dto, esDto, ws, reportNo) => dto.Standard!;
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["J3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                    var sample = ws.Cells["D3"].Value?.ToString();

                    var cellOrder = new List<string> { "A5", "A6", "A7", "A8", "A9", "A10", "A11", "A12", "A13", "A14", "A15", "A16" };
                    var reasonCellOrder = cellOrder.Select(c => "Y" + c.Substring(1)).ToList();
                    if (sample.ToLower().Contains("shell"))
                    {
                        reasonCellOrder = cellOrder.Select(c => "J" + c.Substring(1)).ToList();
                    }
                    if (sample.ToLower().Contains("lining"))
                    {
                        reasonCellOrder = cellOrder.Select(c => "Y" + c.Substring(1)).ToList();
                    }
                    var descMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Side"] = "Side Seam",
                        ["Sleeve"] = "Sleeve Seam",
                        ["Armhole"] = "Armhole Seam",
                        ["Shoulder"] = "Shoulder Seam",
                        ["Armprit"] = "Armprit Seam",
                        ["Front Panel"] = "Front Panel Seam",
                        ["Back Panel"] = "Back Panel Seam",
                        ["OutSide"] = "Out-Side Seam",
                        ["InSide"] = "In-Side Seam",
                        ["Back Rise"] = "Back Rise Seam",
                        ["Front Crotch"] = "Front Crotch Seam",
                        ["Cross"] = "Cross Seam",
                    };

                    var seamInfos = esDto.SeamParameter
                                      ?.FirstOrDefault(s => s.Sample == sample)   // 找到当前行样本
                                      ?.LocationInfos
                                      ?.Where(x => !string.IsNullOrWhiteSpace(x.Location)) // 去掉空Location
                                      .ToList();
                    if (seamInfos?.Count > 0)
                    {
                        for (int i = 0; i < seamInfos.Count && i < cellOrder.Count; i++)
                        {
                            string location = seamInfos[i].Location!.Trim();
                            if (descMap.TryGetValue(location, out var desc))
                            {
                                string cell = cellOrder[i];
                                map[cell] = (wp,dto, esDto, ws, reportNo) => desc;   // 填入对应描述
                            }
                        }
                    }

                    for (int i = 0; i < seamInfos!.Count && i < cellOrder.Count; i++)
                    {
                        var info = seamInfos[i];
                        string location = info.Location!.Trim();

                        // 1. 填描述（原逻辑）
                        if (descMap.TryGetValue(location, out var desc))
                        {
                            string cell = cellOrder[i];
                            map[cell] = (wp, dto, esDto, ws, reportNo) => desc;
                        }

                        // 2. 当 IsNA == false 时，把 Reason 写到同行 J 列
                        if (info.IsNA == true && !string.IsNullOrWhiteSpace(info.Reason))
                        {
                            string reasonCell = reasonCellOrder[i];
                            string reason = "N/A；" + info.Reason;         // 捕获局部变量
                            map[reasonCell] = (wp,dto, esDto, ws, reportNo) => reason;
                        }
                    }
                }
                return map;
            },
            ["Seam Strength"] = (wp,dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (wp,dto, esDto, ws, reportNo) => reportNo;
                if (dto.sampleDescription!.Contains("Fabric")) 
                {
                    map["A19"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                }
               else  if (dto.sampleDescription!.Contains("Garment") && dto.sampleDescription!.Contains("Knit"))
                {
                    map["J5"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                    var sample = ws.Cells["D5"].Value?.ToString();

                    var cellOrder = new List<string> { "A7", "A8", "A9", "A10", "A11", "A12", "A13", "A14", "A15", "A16", "A17", "A18" };
                    var reasonCellOrder = cellOrder.Select(c => "Y" + c.Substring(1)).ToList();
                    if (sample.ToLower().Contains("shell"))
                    {
                        reasonCellOrder = cellOrder.Select(c => "J" + c.Substring(1)).ToList();
                    }
                    if (sample.ToLower().Contains("lining"))
                    {
                        reasonCellOrder = cellOrder.Select(c => "Y" + c.Substring(1)).ToList();
                    }
                    var descMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Side"] = "Side Seam",
                        ["Sleeve"] = "Sleeve Seam",
                        ["Armhole"] = "Armhole Seam",
                        ["Shoulder"] = "Shoulder Seam",
                        ["Armprit"] = "Armprit Seam",
                        ["Front Panel"] = "Front Panel Seam",
                        ["Back Panel"] = "Back Panel Seam",
                        ["OutSide"] = "Out-Side Seam",
                        ["InSide"] = "In-Side Seam",
                        ["Back Rise"] = "Back Rise Seam",
                        ["Front Crotch"] = "Front Crotch Seam",
                        ["Cross"] = "Cross Seam",
                    };
                    var seamInfos = esDto.SeamParameter
                                      ?.FirstOrDefault(s => s.Sample == sample)   // 找到当前行样本
                                      ?.LocationInfos
                                      ?.Where(x => !string.IsNullOrWhiteSpace(x.Location)) // 去掉空Location
                                      .ToList();
                    if (seamInfos?.Count > 0)
                    {
                        for (int i = 0; i < seamInfos.Count && i < cellOrder.Count; i++)
                        {
                            string location = seamInfos[i].Location!.Trim();
                            if (descMap.TryGetValue(location, out var desc))
                            {
                                string cell = cellOrder[i];
                                map[cell] = (wp, dto, esDto, ws, reportNo) => desc;   // 填入对应描述
                            }
                        }
                    }

                    for (int i = 0; i < seamInfos!.Count && i < cellOrder.Count; i++)
                    {
                        var info = seamInfos[i];
                        string location = info.Location!.Trim();

                        // 1. 填描述（原逻辑）
                        if (descMap.TryGetValue(location, out var desc))
                        {
                            string cell = cellOrder[i];
                            map[cell] = (wp, dto, esDto, ws, reportNo) => desc;
                        }

                        // 2. 当 IsNA == false 时，把 Reason 写到同行 J 列
                        if (info.IsNA == true && !string.IsNullOrWhiteSpace(info.Reason))
                        {
                            string reasonCell = reasonCellOrder[i];
                            string reason = "N/A；" + info.Reason;         // 捕获局部变量
                            map[reasonCell] = (wp,dto, esDto, ws, reportNo) => reason;
                        }
                    }
                }
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["J18"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                    var sample = ws.Cells["D3"].Value?.ToString();
                    var cellOrder = new List<string> { "A20", "A21", "A22", "A23", "A24", "A25", "A26", "A27", "A28", "A29", "A30", "A31" };
                    var reasonCellOrder = cellOrder.Select(c => "Y" + c.Substring(1)).ToList();
                    if (sample.ToLower().Contains("shell"))
                    {
                        reasonCellOrder = cellOrder.Select(c => "J" + c.Substring(1)).ToList();
                    }
                    if (sample.ToLower().Contains("lining"))
                    {
                        reasonCellOrder = cellOrder.Select(c => "Y" + c.Substring(1)).ToList();
                    }
                    var descMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Side"] = "Side Seam",
                        ["Sleeve"] = "Sleeve Seam",
                        ["Armhole"] = "Armhole Seam",
                        ["Shoulder"] = "Shoulder Seam",
                        ["Armprit"] = "Armprit Seam",
                        ["Front Panel"] = "Front Panel Seam",
                        ["Back Panel"] = "Back Panel Seam",
                        ["OutSide"] = "Out-Side Seam",
                        ["InSide"] = "In-Side Seam",
                        ["Back Rise"] = "Back Rise Seam",
                        ["Front Crotch"] = "Front Crotch Seam",
                        ["Cross"] = "Cross Seam",
                    };
                    var seamInfos = esDto.SeamParameter
                                      ?.FirstOrDefault(s => s.Sample == sample)   // 找到当前行样本
                                      ?.LocationInfos
                                      ?.Where(x => !string.IsNullOrWhiteSpace(x.Location)) // 去掉空Location
                                      .ToList();
                    if (seamInfos?.Count > 0)
                    {
                        for (int i = 0; i < seamInfos.Count && i < cellOrder.Count; i++)
                        {
                            string location = seamInfos[i].Location!.Trim();
                            if (descMap.TryGetValue(location, out var desc))
                            {
                                string cell = cellOrder[i];
                                map[cell] = (wp,dto, esDto, ws, reportNo) => desc;   // 填入对应描述
                            }
                        }
                    }

                    for (int i = 0; i < seamInfos!.Count && i < cellOrder.Count; i++)
                    {
                        var info = seamInfos[i];
                        string location = info.Location!.Trim();

                        // 1. 填描述（原逻辑）
                        if (descMap.TryGetValue(location, out var desc))
                        {
                            string cell = cellOrder[i];
                            map[cell] = (wp,dto, esDto, ws, reportNo) => desc;
                        }

                        // 2. 当 IsNA == false 时，把 Reason 写到同行 J 列
                        if (info.IsNA == true && !string.IsNullOrWhiteSpace(info.Reason))
                        {
                            string reasonCell = reasonCellOrder[i];
                            string reason = "N/A；" + info.Reason;          // 捕获局部变量
                            map[reasonCell] = (wp,dto, esDto, ws, reportNo) => reason;
                        }
                    }
                }
                return map;
            },
            ["Peel Bond"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>> 
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Tensile Strength"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["I3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Tear Strength"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["F3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Zipper Strength"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Resistance to Snapping of Snap Fasteners"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A26"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Resistance to Unsnapping of Snap Fasteners"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Attachment Strength"] = (wp, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                if (dto.Standard!.Contains("EN 71") || dto.Standard!.Contains("16792"))
                {
                    map["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                    map["A26"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                }
                else
                {
                    map["A3"] = (wp, dto, esDto, ws, reportNo) => "BS EN 17394-2:2020";
                    map["A26"] = (wp, dto, esDto, ws, reportNo) => "CEN/TS 17394-3:2021";
                }
                return map;
            },
            ["Air Permeability"] = (wp, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                map["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                map["F5"] = (wp, dto, esDto, ws, reportNo) => "100";
                map["E6"] = (wp, dto, esDto, ws, reportNo) => "20";
                if (wp.WashingProcedure!.Contains("Machine"))
                {
                    map["A29"] = (wp, dto, esDto, ws, reportNo) => "√";
                    map["P30"] = (wp, dto, esDto, ws, reportNo) => wp.Cycle + " Cycle";
                    map["AF29"] = (wp, dto, esDto, ws, reportNo) => wp.Temperature!;
                    map["O29"] = (wp, dto, esDto, ws, reportNo) => wp.Program!;
                    map["D30"] = (wp, dto, esDto, ws, reportNo) => wp.Detergent!;
                    map["Y30"] = (wp, dto, esDto, ws, reportNo) => wp.DryProcedure!;
                    map["U29"] = (wp, dto, esDto, ws, reportNo) => wp.DryCondition!;
                    map["AG30"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.Iron!) == true ? "/ Iron" : wp.IronMethod!;
                    map["A33"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                }
                else if (wp.WashingProcedure!.Contains("Hand"))
                {
                    map["A32"] = (wp, dto, esDto, ws, reportNo) => "√";
                    map["Z32"] = (wp, dto, esDto, ws, reportNo) => wp.Temperature!;
                    map["AF32"] = (wp, dto, esDto, ws, reportNo) => wp.DryProcedure!;
                    map["A33"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                }
                return map;
            },
            ["Absorbency"] = (wp, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                map["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                if (wp.WashingProcedure!.Contains("Machine"))
                {
                    map["A39"] = (wp, dto, esDto, ws, reportNo) => "√";
                    map["P40"] = (wp, dto, esDto, ws, reportNo) => wp.Cycle + " Cycle";
                    map["AF39"] = (wp, dto, esDto, ws, reportNo) => wp.Temperature!;
                    map["O39"] = (wp, dto, esDto, ws, reportNo) => wp.Program!;
                    map["D40"] = (wp, dto, esDto, ws, reportNo) => wp.Detergent!;
                    map["Y40"] = (wp, dto, esDto, ws, reportNo) => wp.DryProcedure!;
                    map["U39"] = (wp, dto, esDto, ws, reportNo) => wp.DryCondition!;
                    map["AG40"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.Iron!) == true ? "/ Iron" : wp.IronMethod!;
                    map["A43"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                }
                else if (wp.WashingProcedure!.Contains("Hand"))
                {
                    map["A42"] = (wp, dto, esDto, ws, reportNo) => "√";
                    map["Z42"] = (wp, dto, esDto, ws, reportNo) => wp.Temperature!;
                    map["AF42"] = (wp, dto, esDto, ws, reportNo) => wp.DryProcedure!;
                    map["A43"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                }
                return map;
            },
            ["Water Repellency-Spray Test"] = (wp, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                map["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                if (wp.WashingProcedure!.Contains("Machine"))
                {
                    map["A20"] = (wp, dto, esDto, ws, reportNo) => "√";
                    map["P21"] = (wp, dto, esDto, ws, reportNo) => wp.Cycle + " Cycle";
                    map["AF20"] = (wp, dto, esDto, ws, reportNo) => wp.Temperature!;
                    map["O20"] = (wp, dto, esDto, ws, reportNo) => wp.Program!;
                    map["D21"] = (wp, dto, esDto, ws, reportNo) => wp.Detergent!;
                    map["Y21"] = (wp, dto, esDto, ws, reportNo) => wp.DryProcedure!;
                    map["U20"] = (wp, dto, esDto, ws, reportNo) => wp.DryCondition!;
                    map["AG21"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.Iron!) == true ? "/ Iron" : wp.IronMethod!;
                    map["A24"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                }
                else if (wp.WashingProcedure!.Contains("Hand"))
                {
                    map["A23"] = (wp, dto, esDto, ws, reportNo) => "√";
                    map["Z23"] = (wp, dto, esDto, ws, reportNo) => wp.Temperature!;
                    map["AF23"] = (wp, dto, esDto, ws, reportNo) => wp.DryProcedure!;
                    map["A424"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                }
                return map;
            },
            ["Torque & Tension"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
            },
            ["Small Parts"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterAatcc, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
            },
        };

        //登记偏移量
        private static readonly Dictionary<string, int> OffsetRule = new()
        {
            ["DS to Washing"] = 4,
            ["DS to Dry-clean"] = 4,
            ["Drying Rate of Fabrics"] =2,
            ["Water Repellency-Spray Test"] =3,
            ["CF to Perspiration"] = 6,
            // 其余不写就代表单写
        };

        private void WriteSamples(
    ExcelWorksheet ws,
    string[] slice,
    int[]? afmap,
    string[] cellAddrs,
    string[]? AfterWashCellAddrs,
    string itemName,
    string sampleDescription)
        {
            int offset = 0;
            if (sampleDescription.Contains("Fabric"))
            {
                offset = OffsetRule.GetValueOrDefault(itemName, 0);
            }

            if (afmap != null && afmap.Length > 0 && AfterWashCellAddrs != null && AfterWashCellAddrs.Length > 0 && itemName == "Appearance")
            {
                for (int i = 0; i < AfterWashCellAddrs.Length; i++)
                {
                    ws.Cells[AfterWashCellAddrs![i]].Value = afmap[0];
                }
            }
            else if (afmap != null && afmap.Length > 0 && (itemName == "DS to Washing" || itemName == "DS to Dry-clean") && !sampleDescription.Contains("Fabric"))
            {
                for (int i = 0; i < AfterWashCellAddrs!.Length; i++)
                {
                    ws.Cells[AfterWashCellAddrs![i]].Value = afmap[0];
                }
            }
            else if (afmap != null && afmap.Length > 0)
            {
                for (int i = 0; i < afmap.Length; i++)
                {
                    ws.Cells[AfterWashCellAddrs![i]].Value = afmap[i];
                }
            }

            if (itemName == "Appearance"||itemName== "Extension and Recovery")
            {
                for (int i = 0; i < cellAddrs.Length; i++)
                {
                    ws.Cells[cellAddrs[i]].Value = slice[0];
                }
            }
            else if (itemName == "Absorbency")
            {
                for (int i = 0; i < slice.Length; i++)
                {
                    ws.Cells[cellAddrs[i]].Value = slice[i];
                    ws.Cells[cellAddrs[i + 6]].Value = slice[i];
                    ws.Cells[cellAddrs[i + 12]].Value = slice[i];
                }
            }
            else 
            {
                for (int i = 0; i < slice.Length; i++)
                {
                    // 写入样本数据到指定的单元格地址
                    ws.Cells[cellAddrs[i]].Value = slice[i];

                    // 如果有偏移量，并且偏移后的单元格地址在范围内
                    if (offset > 0 && i + offset < cellAddrs.Length)
                    {
                        ws.Cells[cellAddrs[i + offset]].Value = slice[i];
                    }
                }
            }
        }
    }
}
