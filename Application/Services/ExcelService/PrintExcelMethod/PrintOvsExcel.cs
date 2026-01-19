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
    public sealed class PrintOvsExcel : IPrintExcelStrategy
    {
        private readonly LabDbContextSec _db;
        public PrintOvsExcel(LabDbContextSec db)
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
            foreach (var row in checkLists)
            {
                if (new[] { "Seam Slippage", "Seam Strength", "Tear Strength", "Tensile Strength", "Martindale Abrasion" , "Back Pocket Application Strength",
                "Belt Loop Application Strength"}
                     .Contains(row.ItemName))
                    checkLists.Add(new CheckListDto
                    {
                        ItemName = "Mass per Unit Area",
                        Standard = "BS EN 12127:1998",
                        Parameter = "Single unit weight",
                        Type = "Physics",
                        Sample = row.Sample,
                        Extra = null,
                        MenuName = menu,
                        sampleDescription = sampleDescription,
                    });
                break;
            }

            foreach (var dto in checkLists)
            {
                Console.WriteLine($"{dto.ItemName} -> {dto.Type}");
                var pkg = dto.Type == "Wet" ? PackageWet : PackagePhy;
                if (TemplateSheetNames.ContainsKey(dto.ItemName!)|| TemplateSheetNamesNormal.ContainsKey(dto.ItemName!))
                    FillSheet(pkg, dto.ItemName!, dto, reportNumber);
            }
            PackageWet.Save();
            PackagePhy.Save();

        }
        private void FillSheet(
            ExcelPackage pkg,
            string itemName,
            CheckListDto dto,
            string reportNo)
        {

            var tplName = new TemplateSelector(TemplateSheetNames, TemplateSheetNamesNormal).GetTemplateName(itemName, dto.sampleDescription!);
            if (itemName == "Physical & Mechanical" || itemName == "Torque & Tension") 
            {
                switch (itemName) 
                {
                    case "Physical & Mechanical":
                        if (dto.Standard!.Contains("EN 71-1:2014+A1:2018 8.4")) tplName = "Attachment Strength";
                        else if( dto.Standard!.Contains("ASTM F963-23")) tplName = "ASTM F963-23";
                        break;
                    case "Torque & Tension":
                        if (dto.Standard!.Contains("16 CFR 1500.51-53")) tplName = "Torque&Tension";
                        else if (dto.Standard!.Contains("EN 71-1:2024+A1:2018")) tplName = "Attachment Strength";
                        break;  
                }
            }

            var template = pkg.Workbook.Worksheets[tplName];
            //<-------------------------------------------------------------------------------------->

            // 2) 计算需要几张 sheet
            var cellAddrs = CellMapper[itemName](itemName, dto.Standard!,dto.sampleDescription!);
            string[]? AfterWashCellAddrs = null;
            if (/*itemName == "Dimensional Stability" ||*/
                itemName == "Stability to Dry Cleaning" ||
                itemName == "Stability to Washing" ||
                itemName == "Appearance-Common" ||
                itemName == "Security of Attachment(Wash)"||
                itemName == "Easycare/Non-Iron"||
                (itemName == "Appearance" && dto.Standard != "PM01") ||
                (itemName == "Spirality" && dto.Standard != "PM01"))
            {
                AfterWashCellAddrs = AfterWashCellMapper[itemName](itemName,dto.Standard!,dto.sampleDescription!);
            }


            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            var samples = dto.Sample!.Split(',').Select(s => s.Trim()).ToArray();
            if (itemName == "Spirality" && dto.Standard == "PM01")
            {
                samples = dto.Sample!
                    .Split(',')
                    .Select(s => s.Trim())
                    .SelectMany(s => new[] { $"{s} - 5 Wash", $"{s} - 23 Wash", $"{s} - 32 Wash", $"{s} - 45 Wash" })
                    .ToArray();
            }

            int[]? afterWashMap = null;
            if (/*itemName == "Dimensional Stability" ||*/
                itemName == "Stability to Dry Cleaning" ||
                itemName == "Stability to Washing" ||
                itemName == "Appearance-Common"||
                itemName == "Security of Attachment(Wash)" ||
                itemName == "Easycare/Non-Iron" ||
                (itemName == "Appearance"&&dto.Standard!="PM01") ||
                (itemName == "Spirality" && dto.Standard != "PM01"))
            {
                var wp = _db.WetParameterIsos
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                if (wp == null) wp = new WetParameterIso();
                string? afterWash = wp!.AfterWash;

                string? iron = wp!.Iron;
                string? ironMethod = wp!.IronMethod;
                samples = SampleNumCounter.GetSample(dto.Sample!, afterWash, iron, ironMethod);
                afterWashMap = SampleNumCounter.ExpandWashNumbers(samples!, afterWash!, iron);
            }
            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            int offset = 0; // 假设没有偏移
            offset = OffsetRule.GetValueOrDefault(itemName, 0);
            int capacity = offset > 0 ? cellAddrs.Length / 2 : cellAddrs.Length; // 根据是否偏移计算每张 Sheet 的实际容量
            if (itemName == "Colour Fastness to Hot Pressing") { capacity = 3; }// 特例处理，实际容量为3
            if (itemName == "Appearance"||itemName== "Appearance-Common") { capacity = 1; }
            if (itemName == "Dimensional Stability"||(itemName=="Stability to Washing"&& !dto.sampleDescription!.Contains("Fabric"))){ capacity = 1; }
            if (itemName == "Easycare/Non-Iron") { capacity = 1; }
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
                WriteSamples(ws, slice, afmap, cellAddrs, AfterWashCellAddrs, itemName, dto.sampleDescription!,dto.Standard!);
                //这里是分割样本的逻辑<-------------------------------------------------------------------------------------->
                // 5) 其余参数
                if (dto.Type == "Wet")
                {
                    var wp = _db.WetParameterIsos
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                    var extraMap = WetExtraMap.GetValueOrDefault(itemName, (wp, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>())(wp!, dto, reportNo);

                    foreach (var kv in extraMap)
                    {
                        // 如果 wp 为 null，提供一个默认值或者跳过某些操作
                        if (wp == null)
                        {
                            var defaultWp = new WetParameterIso();
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
                    var wp = _db.WetParameterIsos
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                    var extraMap = PhyExtraMap.GetValueOrDefault(itemName, (wp, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>())(wp, dto, reportNo);
                    foreach (var kv in extraMap)
                    {
                        // 如果 wp 为 null，提供一个默认值或者跳过某些操作
                        if (wp == null)
                        {
                            var defaultWp = new WetParameterIso();
                            ws.Cells[kv.Key].Value = kv.Value(defaultWp, dto, reportNo);
                        }
                        else
                        {
                            ws.Cells[kv.Key].Value = kv.Value(wp, dto, reportNo);
                        }
                    }
                }
            }
        }


        private static readonly Dictionary<string, string> TemplateSheetNamesNormal = new()
        {
            ["Water Permeability/Hydrostatic Head"] = "Hydrostatic",
            ["Spray Test"] = "Water Repellency",
            ["Air Permeability"] = "Air Permeability",
            ["Absorbency"] = "Absorbency",
            ["Moisture Management"] = "Moisture Management",
            ["Pilling Resistance"] = "Pilling Resistance",
            ["Abrasion Resistance"] = "Abrasion Resistance",
            ["Tear Strength"] = "Tearing Strength",
            ["Tensile Strength"] = "Tensile Strength",
            ["Stretch & Recovery"] = "Stretch&Recovery of Elastic",
            ["Slide Fastness(Zipper)"] = "Zipper Strength",
            ["Pull Test"] = "Attachment Strength",
            ["Vertical Wicking"] = "Wicking",
            ["Weight per Square Meter"]= "Weight",
            ["Fabric Width"] = "Fabric Width",
            ["Drying Rate"] = "DryingRate",
            ["Electrostatic Properties"]= "Electrostatic Properties",

            ["Colour Fastness to Chlorinated Water"] = "CFtoSublimation&HotPressing&Cl",
            ["Colour Fastness to Chlorine Bleach"] = "CFtoPerspiration&Bleach",
            ["Colour Fastness to Dry Cleaning"] = "Yellowing&DryClean",
            ["Colour Fastness to Hot Pressing"] = "CFtoSublimation&HotPressing&Cl",
            ["Colour Fastness to Light"] = "CFtoWash&Rub&Lig&Wat",
            ["Colour Fastness to Non Chlorine Bleach"] = "CFtoPerspiration&Bleach",
            ["Colour Fastness to Perspiration"] = "CFtoPerspiration&Bleach",
            ["Colour Fastness to PVC Migration"] = "CFtoSeaWater&PVC",
            ["Colour Fastness to Rubbing"] = "CFtoWash&Rub&Lig&Wat",
            ["Colour Fastness to Saliva"] = "CFtoSaliva&Sweat",
            ["Colour Fastness to Saliva and Perspiration"] = "CFtoSaliva&Sweat",
            ["Colour Fastness to Sea Water"] = "CFtoSeaWater&PVC",
            ["Colour Fastness to Washing"] = "CFtoWash&Rub&Lig&Wat",
            ["Colour Fastness to Water"] = "CFtoWash&Rub&Lig&Wat",
            ["Dimensional and Bra Wire Casing Stability"] = "BraWireCasing",
            ["Dye Transfer in Storage"] = "TSBoardFit&DyeTransfer",
            ["Easycare/Non-Iron"] = "Easycare&Non-Iron",
            ["Phenolic Yellowing"] = "Yellowing&DryClean",
            ["Print / Motif / Flock Durability"] = "Print&Motif&Flock",
            ["Print Durability"] = "Print&Motif&Flock",
            ["Security of Attachment(Wash)"] = "Determination of FC",
            ["Stability to Dry Cleaning"] = "StabilitytoDryClean",
            ["TS Board Fit"] = "TSBoardFit&DyeTransfer",
            ["Appearance"] = "Appearance-PM01",
            ["Appearance-Common"] = "Appearance-Common",
            ["Colour Change and Staining"] = "Appearance-PM01",
            };

        private static readonly Dictionary<string, Dictionary<string, string>> TemplateSheetNames = new()
        {
            [("Seam Slippage")] = new Dictionary<string, string>
            {
                {"Fabric", "Seam Slippage&Strength" },
                {"Garment","Seam Slippage&Strength-G"},
            },
            [("Seam Strength")] = new Dictionary<string, string>
            {
                {"Fabric", "Seam Slippage&Strength" },
                {"Garment","Seam Slippage&Strength-G"},
            },
            [("Bursting Strength")] = new Dictionary<string, string>
            {
                {"Fabric", "Bursting Strength" },
                {"Garment","Bursting Strength-G"},
            },
            [("Spirality")] = new Dictionary<string, string>
            {
                {"Fabric", "Spirality-F" },
                {"Garment", "Spirality-G" },
            },
            [("Dimensional Stability")] = new Dictionary<string, string>
            {
                {"Fabric", "PM01Washing-F" },
                {"Garment", "PM01Washing-G" },
                {"Socks", "PM01Washing-Acc" },
                {"Gloves", "PM01Washing-Acc" },
                {"Cap", "PM01Washing-Acc" },
            },
            [("Stability to Washing")] = new Dictionary<string, string>
            {
                {"Fabric", "DStoWashing-F" },
                {"Garment", "DStoWashing-G" },
                {"Socks", "DStoWashing-Acc" },
                {"Gloves", "DStoWashing-Acc" },
                {"Cap", "DStoWashing-Acc" },
            },
        };
        private static readonly Dictionary<string, Func<string, string, string, string[]>> CellMapper = new()
        {
            ["Abrasion of Knitted Footwear Garments - Modified Martindale"] = (n, m, l) => ExcelPrimarkMapper.MapAbrasion(m),

        };
        //取洗涤遍数映射地址的函数
        private static readonly Dictionary<string, Func<string,string, string, string[]>> AfterWashCellMapper = new()
        {
            ["Stability to Washing"] = (n, m, l) => ExcelPrimarkMapper.StabilityAf(l),
        };
        private static readonly Dictionary<string, Func<WetParameterIso, CheckListDto, string, Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>>> WetExtraMap = new()
        {
            ["Colour Fastness to Chlorinated Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["H1"] = (w, dto, reportNo) => reportNo,
                ["A27"] = (w, dto, reportNo) => dto.Standard!,
                ["E28"] = (w, dto, reportNo) => dto.Parameter!,
            },
            ["Colour Fastness to Chlorine Bleach"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A29"] = (w, dto, reportNo) => dto.Standard!,
                ["L30"] = (w, dto, reportNo) => dto.Parameter == "N/A" ? "N/A" : "-",
            },
            ["Colour Fastness to Dry Cleaning"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR12"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Colour Fastness to Hot Pressing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["H1"] = (w, dto, reportNo) => reportNo,
                ["A12"] = (w, dto, reportNo) => dto.Standard!,
                ["G13"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod) ? "/" : w.Temperature!,
                ["R13"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod) ? "N/A" : "-",
            },
            ["Colour Fastness to Light"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A28"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Colour Fastness to Non Chlorine Bleach"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A29"] = (w, dto, reportNo) => dto.Standard!,
                ["L30"] = (w, dto, reportNo) =>dto.Parameter == "N/A"? "N/A" : "-",
            },
            ["Colour Fastness to Perspiration"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Colour Fastness to PVC Migration"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Colour Fastness to Rubbing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A20"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Colour Fastness to Saliva"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["G3"] = (w, dto, reportNo) => "√"
            },
            ["Colour Fastness to Saliva and Perspiration"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["G3"] = (w, dto, reportNo) => "√"
            },
            ["Colour Fastness to Sea Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A10"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Colour Fastness to Washing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["B4"] = (w, dto, reportNo) => w.Program!,
                ["E4"] = (w, dto, reportNo) => w.Temperature!,
                ["L5"] = (w, dto, reportNo) => w.SteelBallNum.ToString()!,
            },
            ["Colour Fastness to Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A35"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Dimensional and Bra Wire Casing Stability"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Dye Transfer in Storage"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR3"] = (w, dto, reportNo) => dto.Standard!,
                ["AY4"] = (w, dto, reportNo) => "30",
                ["BE4"] = (w, dto, reportNo) => "48"
            },
            ["Easycare/Non-Iron"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["BC1"] = (wp, dto, reportNo) => reportNo;
                switch (dto.Standard) 
                {
                    case "AATCC TM124-2018te":
                        map["AR4"] = (wp, dto, reportNo) => dto.Standard!;
                        break;
                    case "ISO7769:2009":
                        map["AR23"] = (wp, dto, reportNo) => dto.Standard!;
                        break;
                }
                return map;
            },
            ["Phenolic Yellowing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Print / Motif / Flock Durability"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR3"] = (w, dto, reportNo) => dto.Standard!,
                ["AU48"] = (w, dto, reportNo) => w.DryProcedure!,
            },
            ["Print Durability"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR3"] = (w, dto, reportNo) => dto.Standard!,
                ["AU48"] = (w, dto, reportNo) => w.DryProcedure!,
            },
            ["Security of Attachment(Wash)"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR4"] = (w, dto, reportNo) => dto.Standard!,
                ["AX52"] = (wp, dto, reportNo) => w.WashingProcedure!,
                ["BW52"] = (wp, dto, reportNo) => w.Temperature!,
                ["BF53"] = (wp, dto, reportNo) => w.Ballast!,
                ["BH54"] = (wp, dto, reportNo) => w.DryProcedure!,
                ["BQ54"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod!) == true ? "/ Iron" : w.IronMethod!,
                ["AR55"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!
            },
            ["Stability to Dry Cleaning"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AW4"] = (w, dto, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal"
            },
            ["TS Board Fit"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR19"] = (w, dto, reportNo) => dto.Standard!
            },
            ["Appearance"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,  ["CM1"] = (w, dto, reportNo) => reportNo,
                ["BC57"] = (w, dto, reportNo) => reportNo,["CM57"] = (w, dto, reportNo) => reportNo,
                ["BC114"] = (w, dto, reportNo) => reportNo, ["CM114"] = (w, dto, reportNo) => reportNo,
                ["BC171"] = (w, dto, reportNo) => reportNo, ["CM171"] = (w, dto, reportNo) => reportNo,
                ["AR3"] = (w, dto, reportNo) => "BS EN ISO 6330 & PM01"!,  ["CB3"] = (w, dto, reportNo) => "BS EN ISO 6330 & PM01",
                ["AR59"] = (w, dto, reportNo) => "BS EN ISO 6330 & PM01",  ["CB59"] = (w, dto, reportNo) => "BS EN ISO 6330 & PM01",
                ["AR116"] = (w, dto, reportNo) => "BS EN ISO 6330 & PM01",  ["CB116"] = (w, dto, reportNo) => "BS EN ISO 6330 & PM01",
                ["AR173"] = (w, dto, reportNo) => "BS EN ISO 6330 & PM01",  ["CB173"] = (w, dto, reportNo) => "BS EN ISO 6330 & PM01",
                ["C1"] = (w, dto, reportNo) => dto.Parameter!,
            },
            ["Colour Change and Staining"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
            },
            ["Dimensional Stability"] = (w, dto, reportNo) => 
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>(); 
                if (dto.sampleDescription!.Contains("Fabric")) 
                {
                    map["BC1"] = (wp, dto, reportNo) => reportNo;
                    map["AR3"] = (wp, dto, reportNo) => "BS EN ISO 5077:2008/BS EN ISO 3759:2011/BS EN ISO 6330:2021";
                    map["AX4"] = (wp, dto, reportNo) => w.WashingProcedure!;
                    map["BX4"] = (wp, dto, reportNo) => w.Temperature!;
                    map["BF5"] = (wp, dto, reportNo) => w.Ballast!;
                    map["BI6"] = (wp, dto, reportNo) => w.DryProcedure!;
                    map["BR6"] = (wp, dto, reportNo) => "/ Iron";
                    map["AR7"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["P1"] = (wp, dto, reportNo) => reportNo;
                    map["A3"] = (wp, dto, reportNo) => "BS EN ISO 5077:2008/BS EN ISO 3759:2011/BS EN ISO 6330:2021";
                    map["I4"] = (wp, dto, reportNo) => w.WashingProcedure!;
                    map["AJ4"] = (wp, dto, reportNo) => w.Temperature!;
                    map["S5"] = (wp, dto, reportNo) => w.Ballast!;
                    map["V6"] = (wp, dto, reportNo) => w.DryProcedure!;
                    map["AE6"] = (wp, dto, reportNo) =>"/ Iron";
                    map["A7"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;

                    map["P52"] = (wp, dto, reportNo) => reportNo;
                    map["A54"] = (wp, dto, reportNo) => "BS EN ISO 5077:2008/BS EN ISO 3759:2011/BS EN ISO 6330:2021";
                    map["I55"] = (wp, dto, reportNo) => w.WashingProcedure!;
                    map["AJ55"] = (wp, dto, reportNo) => w.Temperature!;
                    map["S56"] = (wp, dto, reportNo) => w.Ballast!;
                    map["V57"] = (wp, dto, reportNo) => w.DryProcedure!;
                    map["AE57"] = (wp, dto, reportNo) => "/ Iron";
                    map["A58"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                if (dto.sampleDescription!.Contains("Cap")|| dto.sampleDescription!.Contains("Socks")|| dto.sampleDescription!.Contains("Gloves"))
                {
                    map["N1"] = (wp, dto, reportNo) => reportNo;
                    map["A3"] = (wp, dto, reportNo) => "BS EN ISO 5077:2008/BS EN ISO 3759:2011/BS EN ISO 6330:2021";
                    map["G4"] = (wp, dto, reportNo) => w.WashingProcedure!;
                    map["AL4"] = (wp, dto, reportNo) => w.Temperature!;
                    map["R5"] = (wp, dto, reportNo) => w.Ballast!;
                    map["T6"] = (wp, dto, reportNo) => w.DryProcedure!;
                    map["AD6"] = (wp, dto, reportNo) =>"/ Iron";
                    map["A7"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;

                    map["N56"] = (wp, dto, reportNo) => reportNo;
                    map["A58"] = (wp, dto, reportNo) => "BS EN ISO 5077:2008/BS EN ISO 3759:2011/BS EN ISO 6330:2021";
                    map["G59"] = (wp, dto, reportNo) => w.WashingProcedure!;
                    map["AL59"] = (wp, dto, reportNo) => w.Temperature!;
                    map["R60"] = (wp, dto, reportNo) => w.Ballast!;
                    map["T61"] = (wp, dto, reportNo) => w.DryProcedure!;
                    map["AD61"] = (wp, dto, reportNo) => "/ Iron";
                    map["A62"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;

                }
                return map;
            },
            ["Stability to Washing"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["BC1"] = (wp, dto, reportNo) => reportNo;
                    map["AR3"] = (wp, dto, reportNo) => dto.Standard!;
                    map["AX4"] = (wp, dto, reportNo) => w.WashingProcedure!;
                    map["BX4"] = (wp, dto, reportNo) => w.Temperature!;
                    map["BF5"] = (wp, dto, reportNo) => w.Ballast!;
                    map["BI6"] = (wp, dto, reportNo) => w.DryProcedure!;
                    map["BR6"] = (wp, dto, reportNo) => "/ Iron";
                    map["AR7"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["P1"] = (wp, dto, reportNo) => reportNo;
                    map["A3"] = (wp, dto, reportNo) => dto.Standard!;
                    map["I4"] = (wp, dto, reportNo) => w.WashingProcedure!;
                    map["AJ4"] = (wp, dto, reportNo) => w.Temperature!;
                    map["S5"] = (wp, dto, reportNo) => w.Ballast!;
                    map["V6"] = (wp, dto, reportNo) => w.DryProcedure!;
                    map["AE6"] = (wp, dto, reportNo) => "/ Iron";
                    map["A7"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                if (dto.sampleDescription!.Contains("Cap") || dto.sampleDescription!.Contains("Socks") || dto.sampleDescription!.Contains("Gloves"))
                {
                    map["N1"] = (wp, dto, reportNo) => reportNo;
                    map["A3"] = (wp, dto, reportNo) => dto.Standard!;
                    map["G4"] = (wp, dto, reportNo) => w.WashingProcedure!;
                    map["AL4"] = (wp, dto, reportNo) => w.Temperature!;
                    map["R5"] = (wp, dto, reportNo) => w.Ballast!;
                    map["T6"] = (wp, dto, reportNo) => w.DryProcedure!;
                    map["AD6"] = (wp, dto, reportNo) => "/ Iron";
                    map["A7"] = (wp, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                return map;
            },
            ["Spirality"] = (w, dto, reportNo) => 
            { 
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["P1"] = (w, dto, reportNo) => reportNo;
                if(dto.sampleDescription!.Contains("Fabric")) map["A3"] = (w, dto, reportNo) => "BS EN ISO 16322-2:2021,Method A"!;
                else if(dto.sampleDescription!.Contains("Garment")) map["A3"] = (w, dto, reportNo) => "BS EN ISO 16322-3:2021,Procedure B"!;
                return map;
            },
        };
        private static readonly Dictionary<string, Func<WetParameterIso, CheckListDto, string, Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>>> PhyExtraMap = new()
        {
            ["Water Permeability/Hydrostatic Head"] = (w, dto, reportNo) => 
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                map["A3"] = (w, dto, reportNo) => dto.Standard!;
                if (dto.Parameter!.Contains("Original")) map["I7"] = (w, dto, reportNo) => "1800";
                else 
                {
                    map["D15"] = (w, dto, reportNo) => dto.Parameter!.Contains("1 Cycle")?"1" 
                    : dto.Parameter.Contains("3 Cycles") ? "3"
                    : dto.Parameter.Contains("5 Cycles") ? "5"
                    :"1";
                    map["I15"] = (w, dto, reportNo) => dto.Parameter!.Contains("2000")?"2000"
                    :dto.Parameter.Contains("3000")?"3000"
                    :dto.Parameter.Contains("5000")?"5000"
                    :"1800";

                    map["G32"]= (w, dto, reportNo) => w.WashingProcedure!;
                    map["AJ32"] = (w, dto, reportNo) => w.Temperature!;
                    map["Q33"] = (w, dto, reportNo) => w.Ballast!;
                    map["L34"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["U34"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod!) == true ? "/ Iron" : w.IronMethod!;
                    map["A35"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
               return map;
            },
            ["Spray Test"] = (w, dto, reportNo) => 
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                map["A3"] = (w, dto, reportNo) => dto.Standard!;
                if (dto.sampleDescription!.Contains("Cycle")) 
                {
                    map["C12"]= (w, dto, reportNo) => dto.Parameter!.Contains("1 Cycle")?"1" : "5";
                    if (!string.IsNullOrEmpty(w.DryCleanProcedure)) map["L25"] = (w, dto, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal";
                    else 
                    {
                        map["G20"] = (w, dto, reportNo) => w.WashingProcedure!;
                        map["AJ20"] = (w, dto, reportNo) => w.Temperature!;
                        map["Q21"] = (w, dto, reportNo) => w.Ballast!;
                        map["L22"] = (w, dto, reportNo) => w.DryProcedure!;
                        map["U22"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod!) == true ? "/ Iron" : w.IronMethod!;
                        map["A23"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                    }
                }
                return map;
            },
            ["Air Permeability"] = (w, dto, reportNo) => 
            { 
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                map["A3"] = (w, dto, reportNo) => dto.Standard!;
                map["F5"] = (w, dto, reportNo) => "100"!;
                map["E6"] = (w, dto, reportNo) => "20"!;
                map["G30"] = (w, dto, reportNo) => w.WashingProcedure!;
                map["AJ30"] = (w, dto, reportNo) => w.Temperature!;
                map["Q31"] = (w, dto, reportNo) => w.Ballast!;
                map["L32"] = (w, dto, reportNo) => w.DryProcedure!;
                map["U32"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod!) == true ? "/ Iron" : w.IronMethod!;
                map["A33"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                return map;
            },
            ["Absorbency"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                map["A3"] = (w, dto, reportNo) => dto.Standard!;
                if (dto.MenuName != "HTL-Y-Slipper")
                {
                    map["D17"]= (w, dto, reportNo) => "1";
                    map["G29"] = (w, dto, reportNo) => w.WashingProcedure!;
                    map["AJ29"] = (w, dto, reportNo) => w.Temperature!;
                    map["Q30"] = (w, dto, reportNo) => w.Ballast!;
                    map["L31"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["U31"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod!) == true ? "/ Iron" : w.IronMethod!;
                    map["A32"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                return map;
            },
            ["Moisture Management"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["AQ1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Pilling Resistance"]  = (w, dto, reportNo) => 
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                if (dto.Standard!.Contains("12945-1"))
                {
                    map["G3"] = (w, dto, reportNo) => dto.Standard!;
                    map["D4"] = (w, dto, reportNo) => "7200 & 10800 revs";
                    if (w.AfterWash!.Contains("Original")) map["A5"] = (w, dto, reportNo) => "√";
                    else
                    {
                        map["T5"] = (w, dto, reportNo) => w.AfterWash!.Contains("1") ? "1" : w.AfterWash.Contains("3") ? "3" : "5";
                        if (!string.IsNullOrEmpty(w.DryCleanProcedure)) map["L36"] = (w, dto, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal";
                        else
                        {
                            map["G31"] = (w, dto, reportNo) => w.WashingProcedure!;
                            map["AJ31"] = (w, dto, reportNo) => w.Temperature!;
                            map["Q32"] = (w, dto, reportNo) => w.Ballast!;
                            map["L33"] = (w, dto, reportNo) => w.DryProcedure!;
                            map["U33"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod!) == true ? "/ Iron" : w.IronMethod!;
                            map["A34"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                        }
                    }
                }
                else if (dto.Standard!.Contains("12945-2"))
                {
                    map["F13"] = (w, dto, reportNo) => dto.Standard!;
                    map["D14"] = (w, dto, reportNo) => "2000 revs";
                    if (w.AfterWash!.Contains("Original")) { map["A15"] = (w, dto, reportNo) => "√"; map["A22"] = (w, dto, reportNo) => "√"; }
                    else
                    {
                        map["T15"] = (w, dto, reportNo) => w.AfterWash!.Contains("1") ? "1" : w.AfterWash.Contains("3") ? "3" : "5";
                        map["T22"] = (w, dto, reportNo) => w.AfterWash!.Contains("1") ? "1" : w.AfterWash.Contains("3") ? "3" : "5";
                        if (!string.IsNullOrEmpty(w.DryCleanProcedure)) map["L36"] = (w, dto, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal";
                        else
                        {
                            map["G31"] = (w, dto, reportNo) => w.WashingProcedure!;
                            map["AJ31"] = (w, dto, reportNo) => w.Temperature!;
                            map["Q32"] = (w, dto, reportNo) => w.Ballast!;
                            map["L33"] = (w, dto, reportNo) => w.DryProcedure!;
                            map["U33"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod!) == true ? "/ Iron" : w.IronMethod!;
                            map["A34"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                        }
                    }
                }
                return map;
            },
            ["Abrasion Resistance"]= (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                map["A3"]= (w, dto, reportNo) => dto.Standard!;
                if (dto.Parameter!.Contains("N/A")) map["AC3"] = (w, dto, reportNo) => "N/A";
                else 
                {
                    map["C5"] = (w, dto, reportNo) => dto.Parameter!.Contains("3KPa")?"3KPa"
                    : dto.Parameter!.Contains("9KPa") ? "9KPa"
                    : "12KPa";
                    map["I5"] = (w, dto, reportNo) => dto.Parameter!.Contains("10000") ? "10000 revs"
                    : dto.Parameter!.Contains("15000") ? "15000 revs"
                    : dto.Parameter!.Contains("20000") ? "20000 revs"
                    : "30000 revs";

                    map["AA5"] = (w, dto, reportNo) => "@3000 revs";
                }
                return map;
            },
            ["Tear Strength"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                if (dto.Standard!.Contains("13937-2")) 
                {
                    map["Q3"] = (w, dto, reportNo) => "Tongue Tear";
                }
                if (dto.Standard!.Contains("13937-1"))
                {
                    map["Q3"] = (w, dto, reportNo) => "Elmendorf";
                }
                map["A3"] = (w, dto, reportNo) => dto.Standard!;
                return map;
            },
            ["Tensile Strength"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                map["A3"] = (w, dto, reportNo) => dto.Standard!;
                return map;
            },
            ["Stretch & Recovery"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                map["A3"] = (w, dto, reportNo) => dto.Standard!;
                if (dto.Parameter!.Contains("N/A")) map["I3"] = (w, dto, reportNo) => "N/A";
                return map;
            },
            ["Slide Fastness(Zipper)"] = (w,dto,reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>> 
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Pull Test"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },


            ["Bursting Strength"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                if (dto.sampleDescription!.Contains("Fabric")) map["I3"] = (w, dto, reportNo) => dto.Standard!;
                else if (dto.sampleDescription!.Contains("Seam")) map["I18"] = (w, dto, reportNo) => dto.Standard!;
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    string? component = SeamExtraHelper.GetExtraField<string>(dto, "component", objIndex: 0);
                    string? layout = SeamExtraHelper.GetExtraField<string>(dto, "layout", objIndex: 0);

                    map["J3"] = (w, dto, reportNo) => dto.Standard!;
                    if (layout!.Contains("Shell") && !string.IsNullOrEmpty(layout)) map["Q4"] = (w, dto, reportNo) => "√";
                    if (layout.Contains("Lining") && !string.IsNullOrEmpty(layout)) map["AF4"] = (w, dto, reportNo) => "√";

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
                    // 2. 固定顺序的单元格列表
                    var cellOrder = new List<string>{
                        "A5", "A6", "A7", "A8", "A9", "A10","A11", "A12","A13","A14", "A15", "A16"
                    };
                    var selectedParts = (component ?? "")
                        .Split('-', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(k => descMap.ContainsKey(k))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    // 4. 按顺序依次填，发完为止
                    for (int i = 0; i < selectedParts.Count && i < cellOrder.Count; i++)
                    {
                        string part = selectedParts[i];
                        string cell = cellOrder[i];
                        string desc = descMap[part];
                        map[cell] = (w, dto, reportNo) => desc;
                    }
                }
                    return map;
            },
            ["Seam Slippage"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["A3"] = (w, dto, reportNo) => dto.Standard!;
                }
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    string? component = SeamExtraHelper.GetExtraField<string>(dto, "component", objIndex: 0);
                    string? layout = SeamExtraHelper.GetExtraField<string>(dto, "layout", objIndex: 0);

                    map["J3"] = (w, dto, reportNo) => dto.Standard!;
                    if (layout!.Contains("Shell") && !string.IsNullOrEmpty(layout)) map["Q4"] = (w, dto, reportNo) => "√";
                    if (layout.Contains("Lining") && !string.IsNullOrEmpty(layout)) map["AF4"] = (w, dto, reportNo) => "√";

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
                    // 2. 固定顺序的单元格列表
                    var cellOrder = new List<string>{
                        "A5", "A6", "A7", "A8", "A9", "A10","A11", "A12","A13","A14", "A15", "A16"
                    };
                    var selectedParts = (component ?? "")
                        .Split('-', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(k => descMap.ContainsKey(k))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    // 4. 按顺序依次填，发完为止
                    for (int i = 0; i < selectedParts.Count && i < cellOrder.Count; i++)
                    {
                        string part = selectedParts[i];
                        string cell = cellOrder[i];
                        string desc = descMap[part];
                        map[cell] = (w, dto, reportNo) => desc;
                    }
                }
                return map;
            },
            ["Seam Strength"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (w, dto, reportNo) => reportNo;
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["A3"] = (w, dto, reportNo) => dto.Standard!;
                }
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    string? component = SeamExtraHelper.GetExtraField<string>(dto, "component", objIndex: 0);
                    string? layout = SeamExtraHelper.GetExtraField<string>(dto, "layout", objIndex: 0);

                    map["J18"] = (w, dto, reportNo) => dto.Standard!;
                    if (layout!.Contains("Shell") && !string.IsNullOrEmpty(layout)) map["Q19"] = (w, dto, reportNo) => "√";
                    if (layout.Contains("Lining") && !string.IsNullOrEmpty(layout)) map["AF19"] = (w, dto, reportNo) => "√";

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
                    // 2. 固定顺序的单元格列表
                    var cellOrder = new List<string>{
                        "A20", "A21", "A22", "A23", "A24", "A25","A26", "A27","A28","A29", "A30", "A31"
                    };
                    var selectedParts = (component ?? "")
                        .Split('-', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(k => descMap.ContainsKey(k))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    // 4. 按顺序依次填，发完为止
                    for (int i = 0; i < selectedParts.Count && i < cellOrder.Count; i++)
                    {
                        string part = selectedParts[i];
                        string cell = cellOrder[i];
                        string desc = descMap[part];
                        map[cell] = (w, dto, reportNo) => desc;
                    }
                }
                return map;
            },
        };



        private static readonly Dictionary<string, int> OffsetRule = new()
        {
            ["Colour Fastness to Perspiration"] = 6,
            //["Dimensional Stability"] = 4,
            ["Stability to Washing"] = 4,
            ["Stability to Dry Cleaning"] = 4,
            ["Colour Fastness to Non Chlorine Bleach"] = 6,
            ["Shower Resistant Claims Spray Rating"] = 3,
            ["Absorbency of Textiles"] = 6,
            ["Waterproof Claims Hydrostatic Head"] = 2
        };
        private void WriteSamples(
            ExcelWorksheet ws,
            string[] slice,
            int[]? afmap,
            string[] cellAddrs,
            string[]? AfterWashCellAddrs,
            string itemName,
            string sampleDescription,
            string standard)
        {
            int offset = OffsetRule.GetValueOrDefault(itemName, 0);
            if ((itemName == "Dimensional Stability"||itemName == "Stability to Washing") && !sampleDescription.Contains("Fabric")) offset = 0;

            if (afmap != null && afmap.Length > 0 
                && AfterWashCellAddrs != null 
                && AfterWashCellAddrs.Length > 0 
                && itemName == "Appearance-Common"
                && standard  != "PM01")
            {
                for (int i = 0; i < AfterWashCellAddrs.Length; i++)
                {
                    ws.Cells[AfterWashCellAddrs![i]].Value = afmap[0];
                }
            }
            else if (afmap != null && afmap.Length > 0 
                && (itemName == "Stability to Washing" /*|| itemName == "Dimensional Stability"*/)
                && !sampleDescription.Contains("Fabric"))
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


            if (itemName == "Appearance"
                ||itemName== "Dimensional and Bra Wire Casing Stability"
                ||itemName == "Appearance-Common" || itemName == "Dimensional Stability")
            {
                for (int i = 0; i < cellAddrs.Length; i++)
                {
                    ws.Cells[cellAddrs[i]].Value = slice[0];
                }
            }
            else if (itemName == "Colour Fastness to Hot Pressing")
            {
                for (int i = 0; i < slice.Length; i++)
                {
                    ws.Cells[cellAddrs[i]].Value = slice[i];
                    ws.Cells[cellAddrs[i + 3]].Value = slice[i];
                    ws.Cells[cellAddrs[i + 6]].Value = slice[i];
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
