using OfficeOpenXml;
using static NX_lims_Softlines_Command_System.Application.Services.Factory.PrintExcelStrategyFactory;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelMapper;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.Helper;
using DocumentFormat.OpenXml.Spreadsheet;
using System.ComponentModel;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelPrintTool;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.PrintExcelMethod
{

    public sealed class PrintLppExcel : IPrintExcelStrategy
    {
        private readonly LabDbContextSec _db;
        public PrintLppExcel(LabDbContextSec db)
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
                    FillSheet(pkg, dto.ItemName!, dto, Dto,reportNumber);
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
            var cellAddrs = CellMapper[itemName](itemName, dto.sampleDescription!,dto.Standard!);
            string[]? AfterWashCellAddrs = null;
            if (itemName == "DS to Washing" || itemName == "Spirality/Skewing")
            {
                AfterWashCellAddrs = AfterWashCellMapper[itemName](itemName, dto.sampleDescription!);
            }


            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            var samples = dto.Sample!.Split(',').Select(s => s.Trim()).ToArray();
            if (itemName == "CF to Washing"&& dto.Parameter!.Contains("5 Washes"))
            {
                samples = dto.Sample!
                    .Split(',')
                    .Select(s => s.Trim())
                    .SelectMany(s => new[] { $"{s}×5 Wash" })
                    .ToArray();
            }



            int[]? afterWashMap = null;
            if (itemName == "DS to Washing"  || itemName == "Spirality/Skewing")
            {
                var wp = _db.WetParameterIsos
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                if (wp == null) wp = new WetParameterIso();
                string? afterWash = wp!.AfterWash;
                if (itemName == "DS to Washing")
                {
                    afterWash = string.Join(", ", dto.Sample!
                        .Split(',')
                        .Select(s => s.Trim())
                        .SelectMany(s => new[] { $"{s}-3 Wash" }));
                }
                if (itemName == "Spirality/Skewing")
                {
                    afterWash = string.Join(", ", dto.Sample!
                        .Split(',')
                        .Select(s => s.Trim())
                        .SelectMany(s => new[] { $"{s}-1 Wash" }));
                }
                string? iron = wp!.Iron;
                string? ironMethod = wp!.IronMethod;
                samples = SampleNumCounter.GetSample(dto.Sample!, afterWash, iron, ironMethod);
                afterWashMap = SampleNumCounter.ExpandWashNumbers(samples!, afterWash!,iron);
            }
            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            int offset = 0; // 假设没有偏移
            offset = OffsetRule.GetValueOrDefault(itemName, 0);
            int capacity = offset > 0 ? cellAddrs.Length / 2 : cellAddrs.Length; // 根据是否偏移计算每张 Sheet 的实际容量
            if (itemName == "Water Repellency-Spray Test") { capacity = 3; }
            if (itemName == "DS to Washing"&&! dto.sampleDescription!.Contains("Fabric")) { capacity = 1; }
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
                //这里是分割样本的逻辑<-------------------------------------------------------------------------------------->
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
                /* 把这段样本写进去,如果有水洗遍数，那么也把水洗遍数写进去 */
                WriteSamples(ws, slice, afmap, cellAddrs, AfterWashCellAddrs, itemName,dto.sampleDescription!);
                //这里是分割样本的逻辑<-------------------------------------------------------------------------------------->
                // 5) 其余参数
                if (dto.Type == "Wet")
                {
                    var wp = _db.WetParameterIsos
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                    var extraMap = WetExtraMap.GetValueOrDefault(itemName, (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>())(wp, dto, esDto, ws, reportNo);

                    foreach (var kv in extraMap)
                    {
                        // 如果 wp 为 null，提供一个默认值或者跳过某些操作
                        if (wp == null)
                        {
                            var defaultWp = new WetParameterIso();
                            ws.Cells[kv.Key].Value = kv.Value(defaultWp, dto, esDto, ws, reportNo);
                        }
                        else
                        {
                            ws.Cells[kv.Key].Value = kv.Value(wp, dto, esDto, ws, reportNo);
                        }
                    }
                }
                else if (dto.Type == "Physics")
                {
                    var wp = _db.WetParameterIsos
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                    var extraMap = PhyExtraMap.GetValueOrDefault(itemName, (wp,dto,esDto,ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>())(wp, dto, esDto, ws, reportNo);
                    foreach (var kv in extraMap)
                    {
                        // 如果 wp 为 null，提供一个默认值或者跳过某些操作
                        if (wp == null)
                        {
                            var defaultWp = new WetParameterIso();
                            ws.Cells[kv.Key].Value = kv.Value(defaultWp, dto, esDto, ws, reportNo);
                        }
                        else
                        {
                            ws.Cells[kv.Key].Value = kv.Value(wp, dto, esDto, ws, reportNo);
                        }
                    }
                }
            }


        }
        private static readonly Dictionary<string, string> TemplateSheetNamesNormal = new()
        {
            ["Weight"] = "Weight",
            ["Pilling Resistance"] = "Pilling Resistance",
            ["Abrasion Resistance"] = "Abrasion Resistance",
            ["Zipper Strength"] = "Zipper Strength-EN 16732",
            ["Tear Strength"] = "Tearing Strength",
            ["Tensile Strength"] = "Tensile Strength",
            ["Water Resistance-Hydrostatic Pressure"] = "Hydroatatic Test",
            ["Water Repellency-Spray Test"] = "Water Repellency",
            ["Extension and Recovery"] = "Stretch&Recovery of Elastic",
            ["Air Permeability"] = "Air Permeability",
            ["Attachment Strength"]= "Attachment Strength",
            ["Quick Dry"] = "DryingRate",

            ["CF to Washing"] = "CFtoWRLS",
            ["CF to Rubbing"] = "CFtoWRLS",
            ["CF to Light"] = "CFtoWRLS",
            ["CF to Sea Water"] = "CFtoWRLS",
            ["CF to Perspiration"] = "CFtoPWD",
            ["CF to Water"] = "CFtoPWD",
            ["CF to Dry-clean"] = "CFtoPWD",
            ["CF to Saliva"] = "CFtoSaliva&Sweat",
            ["CF to Sweat"] = "CFtoSaliva&Sweat",
            ["CF to Chlorinated Water"] = "CFtoOrganic&Cl",
            ["CF to Organic Solvents"] = "CFtoOrganic&Cl",
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
            ["Seam Slippage"] = new Dictionary<string[], string>
            {
                { new[]{"Fabric" }, "Seam Slippage&Strength" },
                { new[]{"Garment" },"Seam Slippage&Strength-G"},
            },
            ["Seam Strength"] = new Dictionary<string[], string>
            {
                { new[]{"Fabric" }, "Seam Slippage&Strength" },
                { new[]{"Knit","Garment" },"Seam Bursting"},
                { new[]{"Garment" },"Seam Slippage&Strength-G"},
            },
            ["Spirality/Skewing"]  = new Dictionary<string[], string>
            {  
                { new[] {"Fabric"} , "Spirality-F" },
                { new[] {"Garment"} , "Spirality-G" },
            }
        };
        private static readonly Dictionary<string, Func<string, string, string,string[]>> CellMapper = new()
        {
            ["Weight"] = (n, m,l) => ExcelLPPMapper.MapWeight(),
            ["Pilling Resistance"] = (n, m, l) => ExcelLPPMapper.MapPilling(l),
            ["Abrasion Resistance"] = (n, m, l) => ExcelLPPMapper.MapAbrasion(),
            ["Zipper Strength"] = (n, m, l) => ExcelLPPMapper.MapZipperStrength(),
            ["Tear Strength"] = (n, m, l) => ExcelLPPMapper.MapTear(),
            ["Tensile Strength"] = (n, m, l) => ExcelLPPMapper.MapTensile(),
            ["Water Resistance-Hydrostatic Pressure"] = (n, m, l) => ExcelLPPMapper.MapHydrostaticPressing(),
            ["Water Repellency-Spray Test"] = (n, m, l) => ExcelLPPMapper.MapRepellency(),
            ["Extension and Recovery"] = (n, m, l) => ExcelLPPMapper.MapExtensionAndRecovery(),
            ["Air Permeability"] = (n, m, l) => ExcelLPPMapper.MapAirPermeability(),
            ["Attachment Strength"] = (n, m, l) => ExcelLPPMapper.MapAttachmentStrength(),
            ["Quick Dry"] = (n, m, l) => ExcelLPPMapper.MapDryRate(),
            ["Seam Slippage"] = (n, m, l) => ExcelLPPMapper.MapSeam(n,m),
            ["Seam Strength"] = (n, m, l) => ExcelLPPMapper.MapSeam(n,m),

            ["CF to Washing"] = (n, m, l) => ExcelLPPMapper.MapCFtoWashing(),
            ["CF to Rubbing"] = (n, m, l) => ExcelLPPMapper.MapCFtoRubbing(),
            ["CF to Light"] = (n, m, l) => ExcelLPPMapper.MapCFtoLight(),
            ["CF to Sea Water"] = (n, m, l) => ExcelLPPMapper.MapCFtoSeaWater(),
            ["CF to Perspiration"] = (n, m, l) => ExcelLPPMapper.MapCFtoPerspiration(),
            ["CF to Water"] = (n, m, l) => ExcelLPPMapper.MapCFtoWater(),
            ["CF to Dry-clean"] = (n, m, l) => ExcelLPPMapper.MapCFtoDC(),
            ["CF to Saliva"] = (n, m, l) => ExcelLPPMapper.MapCFtoSalivaSweat(),
            ["CF to Sweat"] = (n, m, l) => ExcelLPPMapper.MapCFtoSalivaSweat(),
            ["CF to Chlorinated Water"] = (n, m, l) => ExcelLPPMapper.MapCFtoCl(),
            ["CF to Organic Solvents"] = (n, m, l) => ExcelLPPMapper.MapCFtoOrganic(),
            ["Spirality/Skewing"] = (n, m, l) => ExcelLPPMapper.MapSpirality(l),
            ["DS to Washing"] = (n, m, l) => ExcelLPPMapper.MapDStoWashing(m),
        };
        //取洗涤遍数映射地址的函数
        private static readonly Dictionary<string, Func<string, string, string[]>> AfterWashCellMapper = new()
        {
            ["DS to Washing"] = (_, m) => ExcelLPPMapper.DStoWashingAf(m),
            ["Spirality/Skewing"] = (_, m) => ExcelLPPMapper.SpiralityAf(),
        };
        private static readonly Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>>> WetExtraMap = new()
        {

            ["DS to Washing"] = (wp, dto, esDto, ws, reportNo) =>
               {
                   var map = new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                   if (dto.sampleDescription!.Contains("Fabric"))
                   {
                       map["BC1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                       map["AR3"] = (wp, dto, esDto, ws, reportNo) => (dto.Standard ?? "").Replace(",", " / ").TrimEnd(' ', '/');
                       map["AX4"] = (wp, dto, esDto, ws, reportNo) => wp.WashingProcedure!;
                       map["BY4"] = (wp, dto, esDto, ws, reportNo) => wp.Temperature!;
                       map["BG5"] = (wp, dto, esDto, ws, reportNo) => wp.Ballast!;
                       map["BI6"] = (wp, dto, esDto, ws, reportNo) => wp.DryProcedure!;
                       map["BR6"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.Iron!) == true ? "/ Iron" : wp.IronMethod!;
                       map["AR7"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                   }
                   else if (dto.sampleDescription!.Contains("Garment"))
                   {
                       map["P1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                       map["A3"] = (wp, dto, esDto, ws, reportNo) => (dto.Standard ?? "").Replace(",", " / ").TrimEnd(' ', '/');
                       map["I4"] = (wp, dto, esDto, ws, reportNo) => wp.WashingProcedure!;
                       map["AJ4"] = (wp, dto, esDto, ws, reportNo) => wp.Temperature!;
                       map["S5"] = (wp, dto, esDto, ws, reportNo) => wp.Ballast!;
                       map["V6"] = (wp, dto, esDto, ws, reportNo) => wp.DryProcedure!;
                       map["AE6"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.Iron!) == true ? "/ Iron" : wp.IronMethod!;
                       map["A7"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                   }
                   else
                   {
                       map["N1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                       map["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                       map["G4"] = (wp, dto, esDto, ws, reportNo) => wp.WashingProcedure!;
                       map["AL4"] = (wp, dto, esDto, ws, reportNo) => wp.Temperature!;
                       map["R5"] = (wp, dto, esDto, ws, reportNo) => wp.Ballast!;
                       map["T6"] = (wp, dto, esDto, ws, reportNo) => wp.DryProcedure!;
                       map["AD6"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.Iron!) == true ? "/ Iron" : wp.IronMethod!;
                       map["A7"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.SpecialCareInstruction) == true ? "-" : wp.SpecialCareInstruction;
                   }
                   return map;
               },
            ["Appearance"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["BC1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["AR4"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
                ["BI13"] = (wp, dto, esDto, ws, reportNo) =>wp.IronMethod!,
                ["BA37"] = (wp, dto, esDto, ws, reportNo) => wp.Temperature!,
                ["BH37"] = (wp, dto, esDto, ws, reportNo) => wp.Detergent!,
                ["AV38"] = (wp, dto, esDto, ws, reportNo) => wp.WashingProcedure!,
                ["BT38"] = (wp, dto, esDto, ws, reportNo) => wp.DryProcedure!,
                ["AR39"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!,
                ["BX39"] = (wp, dto, esDto, ws, reportNo) => wp.Program!,
            },
            ["CF to Chlorinated Water"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["H1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!
            },
            ["CF to Organic Solvents"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["H1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A11"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!
            },
            ["CF to Washing"]= (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>> 
            {
                ["D1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
                ["B4"] = (wp, dto, esDto, ws, reportNo) => wp.Program!,
                ["E4"] = (wp, dto, esDto, ws, reportNo) => wp.Temperature!,
                ["L5"] = (wp, dto, esDto, ws, reportNo) => wp.SteelBallNum!.ToString()!,
            },
            ["CF to Rubbing"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["D1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A20"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["CF to Light"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["D1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A27"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
                ["B30"] = (wp, dto, esDto, ws, reportNo) => dto.Parameter!,
            },
            ["CF to Water"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["D1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A25"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["CF to Perspiration"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["D1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["CF to Dry-clean"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["D1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A37"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["CF to Saliva"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["D1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
                ["G3"] = (wp, dto, esDto, ws, reportNo) => "√"
            },
            ["CF to Sweat"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["D1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,       
                ["J3"] = (wp, dto, esDto, ws, reportNo) => "√"
            },
            ["CF to Sea Water"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["D1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A35"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Spirality/Skewing"] = (wp, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["P1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                    map["A3"] = (wp, dto, esDto, ws, reportNo) => "ISO 16322-2:2021 Method A, Option 1";
                    map["I34"] = (wp, dto, esDto, ws, reportNo) => wp.WashingProcedure!;
                    map["AL34"] = (wp, dto, esDto, ws, reportNo) => wp.Temperature!;
                    map["S35"] = (wp, dto, esDto, ws, reportNo) => wp.Ballast!;
                    map["V36"] = (wp, dto, esDto, ws, reportNo) => wp.DryProcedure!;
                    map["AE36"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.Iron!) == true ? "/ Iron" : wp.IronMethod!;
                    map["A37"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                }
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["P1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                    map["A3"] = (wp, dto, esDto, ws, reportNo) => "ISO 16322-3:2021 Method 2, Option 3";
                    map["I33"] = (wp, dto, esDto, ws, reportNo) => wp.WashingProcedure!;
                    map["AJ33"] = (wp, dto, esDto, ws, reportNo) => wp.Temperature!;
                    map["S34"] = (wp, dto, esDto, ws, reportNo) => wp.Ballast!;
                    map["V35"] = (wp, dto, esDto, ws, reportNo) => wp.DryProcedure!;
                    map["AE35"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.Iron!) == true ? "/ Iron" : wp.IronMethod!;
                    map["A36"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                }
                return map;
            },
        };
        private static readonly Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>>> PhyExtraMap = new()
        {
            ["Weight"]= (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["J1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Pilling Resistance"] = (wp, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                if (dto.Standard!.Contains("12945-2"))
                {
                    map["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                    map["F13"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                    map["D14"] = (wp, dto, esDto, ws, reportNo) => "2000 revs";
                }
                else if (dto.Standard!.Contains("12945-1"))
                {
                    map["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                    map["G3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                    map["D4"] = (wp, dto, esDto, ws, reportNo) => dto.Parameter!;
                }
                return map;
            },
            ["Zipper Strength"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Abrasion Resistance"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Tear Strength"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Tensile Strength"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Extension and Recovery"] = (wp, dto, esDto, ws, reportNo) => 
            {
                var map = new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                map["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                map["AC7"] = (wp, dto, esDto, ws, reportNo) => dto.Parameter!.Contains("N/A")?"N/A":"";
                if (dto.sampleDescription!.Contains("Woven") && !dto.Parameter!.Contains("N/A"))
                {
                    map["F7"] = (wp, dto, esDto, ws, reportNo) => "30";
                    map["A5"] = (wp, dto, esDto, ws, reportNo) => dto.sampleDescription!.Contains("Loop") ?
                    "Woven/Non-woven Fabric: method B---Loop trials Perimeter =200mm Speed =100mm/min" 
                    : "Woven/Non-woven Fabric: method A---Stripe trials  Guage length=200mm  Speed =200mm/min.";
                }
                else if (dto.sampleDescription!.Contains("Knit") && !dto.Parameter!.Contains("N/A")) 
                {
                    map["A5"] = (wp, dto, esDto, ws, reportNo) => dto.sampleDescription!.Contains("Loop") ?
                    "Knitted Fabric: method B---Loop trials  Perimeter =200mm Speed =500mm/min" :
                    "Knitted Fabric: method A---Stripe trials Guage length=100mm Speed =500mm/min.";
                    map["F7"] = (wp, dto, esDto, ws, reportNo) =>
                    dto.sampleDescription!.Contains("3")?"3"
                    : dto.sampleDescription!.Contains("4") ? "4"
                    : dto.sampleDescription!.Contains("5") ? "5"
                    : dto.sampleDescription!.Contains("6") ? "6"
                    : dto.sampleDescription!.Contains("7") ? "7"
                    : dto.sampleDescription!.Contains("8") ? "8"
                    : dto.sampleDescription!.Contains("10") ? "10"
                    :"14";
                }
                map["L7"] = (wp, dto, esDto, ws, reportNo) => "5";
                return map;
            },
            ["Water Resistance-Hydrostatic Pressure"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
                ["I9"] = (wp, dto, esDto, ws, reportNo) => "2000",
                ["I17"] = (wp, dto, esDto, ws, reportNo) => "2000",
                ["D17"] = (wp, dto, esDto, ws, reportNo) => "5",
                ["G26"] = (wp, dto, esDto, ws, reportNo) => wp.WashingProcedure!,
                ["AJ26"] = (wp, dto, esDto, ws, reportNo) => wp.Temperature!,
                ["Q27"] = (wp, dto, esDto, ws, reportNo) => wp.Ballast!,
                ["S28"] = (wp, dto, esDto, ws, reportNo) => wp.DryProcedure!,
                ["AB28"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.Iron!) == true ? "/ Iron" : wp.IronMethod!,
                ["A29"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!,
            },
            ["Water Repellency-Spray Test"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
                ["C12"] = (wp, dto, esDto, ws, reportNo) => "5",
                ["G20"] = (wp, dto, esDto, ws, reportNo) => wp.WashingProcedure!,
                ["AJ20"] = (wp, dto, esDto, ws, reportNo) => wp.Temperature!,
                ["Q21"] = (wp, dto, esDto, ws, reportNo) => wp.Ballast!,
                ["S22"] = (wp, dto, esDto, ws, reportNo) => wp.DryProcedure!,
                ["AB22"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.Iron!) == true ? "/ Iron" : wp.IronMethod!,
                ["A23"] = (wp, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!,
            },
            ["Quick Dry"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["J1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Air Permeability"] = (wp, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo,
                ["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!,
                ["F5"] = (wp, dto, esDto, ws, reportNo) => "100",
                ["E6"] = (wp, dto, esDto, ws, reportNo) => "20",
            },
            ["Attachment Strength"] = (wp, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                if (dto.Standard!.Contains("EN 71") || dto.Standard!.Contains("16792"))
                {
                    map["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                    map["A18"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
                }
                else
                {
                    map["A3"] = (wp, dto, esDto, ws, reportNo) => "BS EN 17394-2:2020";
                    map["A18"] = (wp, dto, esDto, ws, reportNo) => "CEN/TS 17394-3:2021";
                }
                return map;
            },
            ["Seam Slippage"] = (wp, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (wp,dto, esDto, ws, reportNo) => esDto.ReportNumber!;
                if (dto.sampleDescription!.Contains("Fabric")) map["A3"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
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
                            string reason = "N/A；"+ info.Reason;          // 捕获局部变量
                            map[reasonCell] = (wp,dto, esDto, ws, reportNo) => reason;
                        }
                    }
                }
                return map;
            },
            ["Seam Strength"] = (wp, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (wp, dto, esDto, ws, reportNo) => reportNo;
                if (dto.sampleDescription!.Contains("Garment") && dto.sampleDescription!.Contains("Knit"))
                {
                    map["J5"] = (wp, dto, esDto, ws, reportNo) => "ISO 13938-2:2019";
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
                            string reason = "N/A；" + info.Reason;       // 捕获局部变量
                            map[reasonCell] = (wp, dto, esDto, ws, reportNo) => reason;
                        }
                    }
                }
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["J18"] = (wp, dto, esDto, ws, reportNo) => dto.Standard!;
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

        };



        private static readonly Dictionary<string, int> OffsetRule = new()
        {
            ["CF to Perspiration"] = 6,
            ["DS to Washing"] = 4,
            ["Water Repellency-Spray Test"] = 3
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
            int offset = OffsetRule.GetValueOrDefault(itemName, 0);
            if (itemName == "DS to Washing" && !sampleDescription.Contains("Fabric")) offset = 0;
            if (afmap != null && afmap.Length > 0 && itemName == "DS to Washing" && !sampleDescription.Contains("Fabric"))
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