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
    public sealed class PrintPepcoExcel : IPrintExcelStrategy
    {
        private readonly LabDbContextSec _db;
        public PrintPepcoExcel(LabDbContextSec db)
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
            var template = pkg.Workbook.Worksheets[tplName];
            // 2) 计算需要几张 sheet
            var cellAddrs = CellMapper[itemName](itemName, dto.sampleDescription!);
            string[]? AfterWashCellAddrs = null;
            if (itemName == "DS to Washing")
            {
                AfterWashCellAddrs = AfterWashCellMapper[itemName](itemName, dto.sampleDescription!);
            }


            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            var samples = dto.Sample!.Split(',').Select(s => s.Trim()).ToArray();
            if (itemName == "Air Permeability" && dto.sampleDescription!.Contains("Breathability")) 
            {
                samples = dto.Sample!
                    .Split(',')
                    .Select(s => s.Trim())
                    .SelectMany(s => new[] { s, $"{s} - After 3 Wash" })
                    .ToArray();
            }
            int[]? afterWashMap = null;
            if (itemName == "DS to Washing")
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
                        .SelectMany(s => new[] { $"{s}-1 Wash" }));
                }

                string? iron = wp!.Iron;
                string? ironMethod = wp!.IronMethod;
                samples = SampleNumCounter.GetSample(dto.Sample!, afterWash, iron, ironMethod);
                afterWashMap = SampleNumCounter.ExpandWashNumbers(samples!, afterWash!, iron);
            }
            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            int offset = 0; // 假设没有偏移
            offset = OffsetRule.GetValueOrDefault(itemName, 0);
            int capacity = offset > 0 ? cellAddrs.Length / 2 : cellAddrs.Length; // 根据是否偏移计算每张 Sheet 的实际容量
            if (itemName == "Air Permeability"&&dto.sampleDescription!.Contains("Breathability")) { capacity = 2; }// 特例处理，实际容量为3
            if (itemName == "Appearance"||itemName=="Print Durability") { capacity = 1; }
            if (itemName == "DS to Washing" && !dto.sampleDescription!.Contains("Fabric")) { capacity = 1; }
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
                WriteSamples(ws, slice, afmap, cellAddrs, AfterWashCellAddrs, itemName, dto.sampleDescription);
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
            ["Pilling Resistance"] = "Pilling Resistance",
            ["Water Resistance-Hydrostatic Pressure"] = "Hydroatatic",
            ["Air Permeability"] = "Air Permeability",
            ["Absorbency"] = "Absorbency",
            ["Attachment Strength"] = "Attachment Strength",
            ["Wicking"] = "Wicking",
            ["Drying Rate of Fabrics"] = "DryingRate",
            ["Water Repellency-Spray Test"] = "Water Repellency",
            ["Attachment Strength"] = "Attachment Strength",
            ["Appearance"] = "AppearanceAfterWashing",
            ["CF to Washing"] = "CFtoWashing&Rubbing&Light",
            ["CF to Rubbing"] = "CFtoWashing&Rubbing&Light",
            ["CF to Light"] = "CFtoWashing&Rubbing&Light",
            ["CF to Perspiration"] = "CFtoPerspiration&Water",
            ["CF to Water"] = "CFtoPerspiration&Water",
            ["Print Durability"] = "Print Durability",
        };
        private static readonly Dictionary<string, Dictionary<string[], string>> TemplateSheetNames = new()
        {
            ["DS to Washing"] = new Dictionary<string[], string>
            {
                {new[] { "Fabric" }, "DStoWashing-F" },
                {new[] { "Garment" }, "DStoWashing-G" },
                {new[] { "Socks" }, "DStoWashing-Acc" },
                {new[] { "Gloves" }, "DStoWashing-Acc" },
                {new[] { "Cap" }, "DStoWashing-Acc" },
            },
            ["Seam Slippage"] = new Dictionary<string[], string>
            {
                {new[] { "Fabric" }, "Seam Slippage" },
                {new[] { "Garment" },"Seam Slippage-G"},
            },
        };
        private static readonly Dictionary<string, Func<string, string, string[]>> CellMapper = new()
        {
            ["Pilling Resistance"] = (_, _) => ExcelPepcoMapper.MapPilling(),
            ["Water Resistance-Hydrostatic Pressure"] = (_, _) => ExcelPepcoMapper.MapHydroatatic(),
            ["Air Permeability"] = (_, _) => ExcelPepcoMapper.MapAir(),
            ["Absorbency"] = (_, _) => ExcelPepcoMapper.MapAbsorbency(),
            ["Attachment Strength"] = (_, _) => ExcelPepcoMapper.MapAttachment(),
            ["Wicking"] = (_, _) => ExcelPepcoMapper.MapWicking(),
            ["Drying Rate of Fabrics"] = (_, _) => ExcelPepcoMapper.MapDryRate(),
            ["Water Repellency-Spray Test"] = (_, m) => ExcelPepcoMapper.MapRepellency(m),
            ["Appearance"] = (_, _) => ExcelPepcoMapper.MapAppearance(),
            ["CF to Washing"] = (n, _) => ExcelPepcoMapper.MapWRL(n),
            ["CF to Rubbing"] = (n, _) => ExcelPepcoMapper.MapWRL(n),
            ["CF to Light"] = (n, _) => ExcelPepcoMapper.MapWRL(n),
            ["CF to Perspiration"] = (n, _) => ExcelPepcoMapper.MapPW(n),
            ["CF to Water"] = (n, _) => ExcelPepcoMapper.MapPW(n),
            ["Print Durability"] = (_, _) => ExcelPepcoMapper.MappPrintDurability(),
            ["DS to Washing"] = (_, m) => ExcelPepcoMapper.MapDStoWashing(m),
            ["Seam Slippage"] = (_, m) => ExcelPepcoMapper.MapSeamSlippage(m),
        };
        //取洗涤遍数映射地址的函数
        private static readonly Dictionary<string, Func<string, string, string[]>> AfterWashCellMapper = new()
        {
            ["DS to Washing"] = (_, m) => ExcelPepcoMapper.DStoWashingAf(m)
        };
        private static readonly Dictionary<string, Func<WetParameterIso, CheckListDto, string, Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>>> WetExtraMap = new()
        {
            ["DS to Washing"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["BC1"] = (w, dto, reportNo) => reportNo;
                    map["AR3"] = (w, dto, reportNo) => dto.Standard!;
                    map["AX4"] = (w, dto, reportNo) => w.WashingProcedure!;
                    map["BY4"] = (w, dto, reportNo) => w.Temperature!;
                    map["BF5"] = (w, dto, reportNo) => w.Ballast!;
                    map["BI6"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["BR6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!;
                    map["AR7"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction) == true ? "-" : w.SpecialCareInstruction;
                }
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["P1"] = (w, dto, reportNo) => reportNo;
                    map["A3"] = (w, dto, reportNo) => dto.Standard!;
                    map["I4"] = (w, dto, reportNo) => w.WashingProcedure!;
                    map["AJ4"] = (w, dto, reportNo) => w.Temperature!;
                    map["S5"] = (w, dto, reportNo) => w.Ballast!;
                    map["V6"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["AE6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!;
                    map["A7"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction) == true ? "-":w.SpecialCareInstruction;
                }
                else
                {
                    map["N1"] = (w, dto, reportNo) => reportNo;
                    map["A3"] = (w, dto, reportNo) => dto.Standard!;
                    map["G4"] = (w, dto, reportNo) => w.WashingProcedure!;
                    map["AL4"] = (w, dto, reportNo) => w.Temperature!;
                    map["R5"] = (w, dto, reportNo) => w.Ballast!;
                    map["T6"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["AD6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!;
                    map["A7"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction) == true ? "-" : w.SpecialCareInstruction;
                }
                return map;
            },
            ["Appearance"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR4"] = (w, dto, reportNo) =>dto.Standard!,
                ["BG6"] = (w, dto, reportNo) => "1",
                ["BE13"] = (w, dto, reportNo) => "1",
                ["BI13"] = (w, dto, reportNo) => w.IronMethod!,
                ["BX39"] = (w, dto, reportNo) => w.Temperature!,
                ["AX39"] = (w, dto, reportNo) => w.WashingProcedure!,
                ["BJ41"] = (w, dto, reportNo) => w.DryProcedure!,
                ["BG40"] = (w, dto, reportNo) => w.Ballast!,
                ["BS41"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!,
                ["AR42"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction) == true ? "-" : w.SpecialCareInstruction,
            },
            ["Print Durability"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR3"] = (w, dto, reportNo) => dto.Standard!,
                ["BG5"] = (w, dto, reportNo) => "1",
                ["BE12"] = (w, dto, reportNo) => "1",
                ["BI12"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!,
                ["BX38"] = (w, dto, reportNo) => w.Temperature!,
                ["AX38"] = (w, dto, reportNo) => w.WashingProcedure!,
                ["BJ40"] = (w, dto, reportNo) => w.DryProcedure!,
                ["BG39"] = (w, dto, reportNo) => w.Ballast!,
                ["BS40"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!,
                ["AR41"] = (w, dto, reportNo) => w.SpecialCareInstruction ?? "-",
            },
            ["CF to Washing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["B4"] = (w, dto, reportNo) => w.Program!,
                ["E4"] = (w, dto, reportNo) => w.Temperature!,
                ["L5"] = (w, dto, reportNo) => w.SteelBallNum!.ToString()!,
            },
            ["CF to Rubbing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A20"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["CF to Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A25"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["CF to Perspiration"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            }
        };

        private static readonly Dictionary<string, Func<WetParameterIso, CheckListDto, string, Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>>> PhyExtraMap = new()
        {
            ["Weight"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["J1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Pilling Resistance"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["F3"] = (w, dto, reportNo) => dto.Standard!,
                ["D4"] = (w, dto, reportNo) => dto.Parameter!,
            },
            ["Water Resistance-Hydrostatic Pressure"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (wp, dto, reportNo) => reportNo,
                ["A3"] = (wp, dto, reportNo) => dto.Standard!,
                ["AJ25"] = (w, dto, reportNo) => w.Temperature!,
                ["P26"] = (w, dto, reportNo) => w.Ballast!,
                ["G25"] = (w, dto, reportNo) => w.WashingProcedure!,
                ["S27"] = (w, dto, reportNo) => w.DryProcedure!,
                ["AB27"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!)== true ? "/ Iron" : w.IronMethod!,
                ["A28"] = (w, dto, reportNo) => w.SpecialCareInstruction ?? "-",
            },
            ["Water Repellency-Spray Test"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (wp, dto, reportNo) => reportNo;
                map["A3"] = (wp, dto, reportNo) => dto.Standard!;
                map["AJ20"] = (w, dto, reportNo) => w.Temperature!;
                map["P21"] = (w, dto, reportNo) => w.Ballast!;
                map["G20"] = (w, dto, reportNo) => w.WashingProcedure!;
                map["S22"] = (w, dto, reportNo) => w.DryProcedure!;
                map["AB22"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!;
                map["A23"] = (w, dto, reportNo) => w.SpecialCareInstruction ?? "-";

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
            ["Air Permeability"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (wp, dto, reportNo) => reportNo;
                map["A3"] = (wp, dto, reportNo) => dto.Standard!;
                map["F5"] = (wp, dto, reportNo) => "100";
                map["E6"] = (wp, dto, reportNo) => "20";
                if (dto.sampleDescription!.Contains("Breathability"))
                {
                    map["AJ31"] = (w, dto, reportNo) => w.Temperature!;
                    map["P32"] = (w, dto, reportNo) => w.Ballast!;
                    map["G31"] = (w, dto, reportNo) => w.WashingProcedure!;
                    map["S33"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["AB33"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!;
                    map["AI33"] = (w, dto, reportNo) => "3 Wash";
                    map["A34"] = (w, dto, reportNo) => w.SpecialCareInstruction ?? "-";
                }
                return map;
            },
            ["Absorbency"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                //["AJ20"] = (w, dto, reportNo) => w.Temperature!,
                //["P21"] = (w, dto, reportNo) => w.Ballast!,
                //["G20"] = (w, dto, reportNo) => w.WashingProcedure!,
                //["S22"] = (w, dto, reportNo) => w.DryProcedure!,
                //["AB22"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod!)== true ? "/ Iron" : w.IronMethod!,
                //["A23"] = (w, dto, reportNo) => w.SpecialCareInstruction ?? "-",
            },
            ["Attachment Strength"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (wp, dto, reportNo) => reportNo;
                if (dto.Standard!.Contains("EN 71") || dto.Standard!.Contains("16792"))
                {
                    map["A3"] = (wp, dto, reportNo) => dto.Standard!;
                    map["A18"] = (wp, dto, reportNo) => dto.Standard!;
                }
                else
                {
                    map["A3"] = (wp, dto, reportNo) => "BS EN 17394-2:2020";
                    map["A18"] = (wp, dto, reportNo) => "CEN/TS 17394-3:2021";
                }
                return map;
            },
            ["Drying Rate of Fabrics"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["J1"] = (wp, dto, reportNo) => reportNo,
                ["A3"] = (wp, dto, reportNo) => dto.Standard!,
            },
            ["Wicking"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["J1"] = (wp, dto, reportNo) => reportNo,
                ["A3"] = (wp, dto, reportNo) => dto.Standard!,
            },
        };



        private static readonly Dictionary<string, int> OffsetRule = new()
        {
            ["CF to Perspiration"] = 6,
            ["DS to Washing"] = 4,
            ["Water Resistance-Hydrostatic Pressure"] = 2,
            ["Water Repellency-Spray Test"] = 3,
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
            if (!sampleDescription.Contains("Fabric") && itemName == "DS to Washing") offset = 0;

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





            if (itemName == "Appearance"|| itemName == "Print Durability")
            {
                for (int i = 0; i < cellAddrs.Length; i++)
                {
                    ws.Cells[cellAddrs[i]].Value = slice[0];
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
