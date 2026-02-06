using OfficeOpenXml;
using static NX_lims_Softlines_Command_System.Application.Services.Factory.PrintExcelStrategyFactory;
using System.ComponentModel;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelMapper;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.Helper;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelPrintTool;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Spreadsheet;

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.PrintExcelMethod
{
    public class PrintJakoExcel : IPrintExcelStrategy
    {
        private readonly LabDbContextSec _db;
        public PrintJakoExcel(LabDbContextSec db)
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
            if (itemName == "DS to Washing" || itemName == "DS to Dry-clean" || (itemName == "Appearance"&&dto.sampleDescription!.Contains("Garment") )|| itemName == "Spirality/Skewing")
            {
                AfterWashCellAddrs = AfterWashCellMapper[itemName](itemName, dto.MenuName!);
            }

            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            var samples = dto.Sample!.Split(',').Select(s => s.Trim()).ToArray();
            int[]? afterWashMap = null;
            if (itemName == "DS to Washing" || itemName == "DS to Dry-clean" || (itemName == "Appearance" && dto.sampleDescription!.Contains("Garment")) || itemName == "Spirality/Skewing")
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

            int offset = 0;
            if (dto.sampleDescription!.Contains("Fabric"))
            {
                offset = OffsetRule.GetValueOrDefault(itemName, 0);
            }// 获取偏移量，默认为0
            int capacity = offset > 0 ? cellAddrs.Length / 2 : cellAddrs.Length; // 根据是否偏移计算每张 Sheet 的实际容量
            if (itemName == "Appearance"||itemName== "Print Durability For JAKO") { capacity = 1; }
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
                    ws = pkg.Workbook.Worksheets.Copy(tplName, newSheetName);
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
                WriteSamples(ws, slice, afmap, cellAddrs, AfterWashCellAddrs, itemName,dto.sampleDescription);

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
                    var extraMap = PhyExtraMap.GetValueOrDefault(itemName, (dto, esDto, ws, reportNo) => new Dictionary<string, Func<CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>())(dto,esDto,ws, reportNo);
                    foreach (var kv in extraMap)
                    {
                        ws.Cells[kv.Key].Value = kv.Value(dto,esDto,ws, reportNo);
                    }
                }
            }


        }
        private static readonly Dictionary<string, string> TemplateSheetNamesNormal = new()
        {
            ["CF to Washing"] = "CFtoWashing&Rubbing&Light",
            ["CF to Rubbing"] = "CFtoWashing&Rubbing&Light",
            ["CF to Light"] = "CFtoWashing&Rubbing&Light",
            ["CF to Light and Perspiration"] = "CFtoWashing&Rubbing&Light",
            ["CF to Perspiration"] = "CFtoPerspiration&Water&Dryclean",
            ["CF to Water"] = "CFtoPerspiration&Water&Dryclean",
            ["CF to Dry-clean"] = "CFtoPerspiration&Water&Dryclean",
            ["CF to Sublimation in Storage"] = "CFtoSublimation&Ironing",
            ["CF to Hot Pressing"] = "CFtoSublimation&Ironing",
            ["CF to Sea Water"] = "CFtoCl&Sea&Yellow",
            ["CF to Chlorinated Water"] = "CFtoCl&Sea&Yellow",
            ["Phenolic Yellowing"] = "CFtoCl&Sea&Yellow",
            ["Print Durability For JAKO"] = "Print Durability",
            ["Heat Press Test For JAKO"] = "Heat Pressing Test",
            ["Weight"] = "Weight",
            ["Pilling Resistance"] = "Pilling Resistance",
            ["Snagging Resistance"] = "Abrasion&Snagging",
            ["Abrasion Resistance"] = "Abrasion&Snagging",
            ["Tensile Strength"] = "Seam Slippage&Tensile",
            ["Tear Strength"] = "Tear Strength",
            ["Extension and Recovery"] = "Stretch&Recovery of Elastic",
            ["Water Repellency-Spray Test"] = "WaterRepellency",
            ["Water Resistance-Hydrostatic Pressure"] = "Hydroatatic Test",

        };
        private static readonly Dictionary<string, Dictionary<string[], string>> TemplateSheetNames = new()
        {
            ["Appearance"] = new Dictionary<string[], string>
            {
                {new[] { "Fabric" }, "AppearanceAfterWashing-F" },
                {new[] { "Garment" },"AppearanceAfterWashing-G"},
            },
            ["DS to Dry-clean"] = new Dictionary<string[], string>
            {
                { new[] {"Fabric" }, "DStoDryclean-F" },
                {new[] { "Garment" }, "DStoDryclean-G" },
                {new[] { "Socks" }, "DStoDryclean-Acc" },
                {new[] { "Gloves" }, "DStoDryclean-Acc" },
                {new[] { "Cap" }, "DStoDryclean-Acc" },
            },
            ["Spirality/Skewing"] = new Dictionary<string[], string>
            {
                {new[] { "Fabric" }, "Spirality-F" },
                {new[] { "Garment" }, "Spirality-G" },
            },
            ["Seam Slippage"] = new Dictionary<string[], string>
            {
                { new[] {"Fabric" }, "Seam Slippage&Tensile" },
                {new[] { "Garment" }, "Seam Slippage&Breakage-G" },
            },
            ["Bursting Strength"] = new Dictionary<string[], string>
            {
                 { new[] {"Fabric" },"Bursting Strength"},
                 { new[] {"Garment" },"Seam Bursting-G"}
            },
            ["Seam Strength"] = new Dictionary<string[], string>
            {
                 { new[] {"Knit" ,"Garment"},"Seam Bursting-G"},
                 {new[] { "Garment" },"Seam Slippage&Breakage-G"}
            },
            ["Zipper Strength"] = new Dictionary<string[], string>
            {
                 {new[] { "EN" },"Zipper Strength-ASTM D2061"},
                 {new[] { "ASTM" },"Zipper Strength-EN 16732"}
            }
        };
        private static readonly Dictionary<string, Func<string, string, string[]>> CellMapper = new()
        {
            ["Appearance"] = (_, m) => ExcelJakoMapper.MapAppearance(m),
            ["DS to Dry-clean"] = (_, m) => ExcelJakoMapper.MapDStoDS(m),
            ["CF to Washing"] = (n, _) => ExcelJakoMapper.MapWRL(n),
            ["CF to Rubbing"] = (n, _) => ExcelJakoMapper.MapWRL(n),
            ["CF to Light"] = (n, _) => ExcelJakoMapper.MapWRL(n),
            ["CF to Light and Perspiration"] = (n, _) => ExcelJakoMapper.MapWRL(n),
            ["CF to Perspiration"] = (n, _) => ExcelJakoMapper.MapPWD(n),
            ["CF to Water"] = (n, _) => ExcelJakoMapper.MapPWD(n),
            ["CF to Dry-clean"] = (n, _) => ExcelJakoMapper.MapPWD(n),
            ["Spirality/Skewing"] = (_, m) => ExcelJakoMapper.MapSpirality(m),
            ["CF to Sublimation in Storage"] = (n, _) => ExcelJakoMapper.MapSI(n),
            ["CF to Hot Pressing"] = (n, _) => ExcelJakoMapper.MapSI(n),
            ["CF to Sea Water"] = (n, _) => ExcelJakoMapper.MapCSY(n),
            ["CF to Chlorinated Water"] = (n, _) => ExcelJakoMapper.MapCSY(n),
            ["Phenolic Yellowing"] = (n, _) => ExcelJakoMapper.MapCSY(n),
            ["Print Durability For JAKO"] = (n, _) => ExcelJakoMapper.MapPrint(n),
            ["Heat Press Test For JAKO"] = (_, _) => ExcelJakoMapper.MapHeat(),
            ["Weight"] = (_, _) => ExcelJakoMapper.WeightMap(),
            ["Pilling Resistance"] = (_, _) => ExcelJakoMapper.PillingMap(),
            ["Seam Slippage"] = (_, m) => ExcelJakoMapper.SeamSlippageMap(m),
            ["Seam Strength"] = (_, m) => ExcelJakoMapper.SeamStrengthMap(m),
            ["Bursting Strength"] = (_, _) => ExcelJakoMapper.BurstingMap(),
            ["Extension and Recovery"] = (_, _) => ExcelJakoMapper.ElasticMap(),
            ["Abrasion Resistance"] = (n, _) => ExcelJakoMapper.ASMap(n),
            ["Snagging Resistance"] = (n, _) => ExcelJakoMapper.ASMap(n),
            ["Tensile Strength"] = (_, _) => ExcelJakoMapper.TensileMap(),
            ["Tear Strength"] = (_, _) => ExcelJakoMapper.TearMap(),
            ["Water Repellency-Spray Test"] = (_, _) => ExcelJakoMapper.WaterRepellencyMap(),
            ["Water Resistance-Hydrostatic Pressure"] = (_, _) => ExcelJakoMapper.HydrostaticMap(),
        };

        //取洗涤遍数映射地址的函数
        private static readonly Dictionary<string, Func<string, string, string[]>> AfterWashCellMapper = new()
        {
            ["Appearance"] = (_, m) => ExcelJakoMapper.AppearanceAf(m),
            ["Spirality/Skewing"] = (_, m) => ExcelJakoMapper.SpiralityAf(m),
        };

        private static readonly Dictionary<string, Func<WetParameterIso, CheckListDto, string, Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>>> WetExtraMap = new()
        {
            ["Appearance"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["BC1"] = (w, dto, reportNo) => reportNo;
                    map["CM1"] = (w, dto, reportNo) => reportNo;
                    map["AR4"] = (w, dto, reportNo) => "EN ISO 5077:2007 / EN ISO 3759:2011 / EN ISO 6330:2021";
                    map["AR12"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                    map["BG6"] = (w, dto, reportNo) => w.Ballast!;
                }
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["P1"] = (w, dto, reportNo) => reportNo;
                    map["AZ1"] = (w, dto, reportNo) => reportNo;
                    map["A6"] = (w, dto, reportNo) => w.WashingProcedure!;
                    map["L5"] = (w, dto, reportNo) => w.Temperature!;
                    map["Y6"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["E7"] = (w, dto, reportNo) => w.Program!.Contains("1:50h") == true ? "1:50h" : w.Program.Contains("1:20h") == true ? "1:20h" : "1h";
                    map["U7"] = (w, dto, reportNo) => w.Program!.Contains("1400") == true ? "1400rpm" : w.Program.Contains("1200") == true ? "1200 rpm" : "600 rpm";
                    map["A8"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                }
                return map;
            },
            ["DS to Dry-clean"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["M1"] = (w, dto, reportNo) => reportNo;
                    map["A3"] = (w, dto, reportNo) => dto.Standard!;
                    map["F4"] = (w, dto, reportNo) => w.Sensitive!;
                }
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["P1"] = (w, dto, reportNo) => reportNo;
                    map["A3"] = (w, dto, reportNo) => dto.Standard!;
                    map["H4"] = (w, dto, reportNo) => w.Sensitive!;
                }
                return map;
            },
            ["CF to Sublimation in Storage"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["D4"] = (w, dto, reportNo) => w.Temperature!,
                ["G4"] = (w, dto, reportNo) => "48",
                ["C7"] = (w, dto, reportNo) => w.Ballast!
            },
            ["CF to Hot Pressing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A12"] = (w, dto, reportNo) => dto.Standard!,
                ["E13"] = (w, dto, reportNo) => w.Temperature!,
                //["L13"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod) ? "N/A" : null,

            },
            ["CF to Washing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["B4"] = (w, dto, reportNo) => w.Program!,
                ["E4"] = (w, dto, reportNo) => w.Temperature!,
                ["L5"] = (w, dto, reportNo) => w.SteelBallNum.ToString()!
            },
            ["CF to Rubbing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A20"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["CF to Light"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A28"] = (w, dto, reportNo) => dto.Standard!,
                ["B31"] = (w, dto, reportNo) =>"L-4",
            },
            ["CF to Light and Perspiration"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A36"] = (w, dto, reportNo) => dto.Standard!,
                ["B38"] = (w, dto, reportNo) => "L-4",
            },
            ["CF to Chlorinated Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["G1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["A4"] = (w, dto, reportNo) => "20",
            },
            ["CF to Sea Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["G1"] = (w, dto, reportNo) => reportNo,
                ["A10"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Phenolic Yellowing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["G1"] = (w, dto, reportNo) => reportNo,
                ["A22"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["CF to Perspiration"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["CF to Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A25"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["CF to Dry-clean"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A37"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Print Durability For JAKO"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["E1"] = (w, dto, reportNo) => reportNo,
            },
            ["Heat Press Test For JAKO"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["D4"] = (w, dto, reportNo) => w.Temperature!,
                ["F4"] = (w, dto, reportNo) => w.Program!,
                ["D33"] = (w, dto, reportNo) => "1",
                ["G33"] = (w, dto, reportNo) => dto.Sample!
            },
            ["Spirality/Skewing"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["P1"] = (w, dto, reportNo) => reportNo;
                //map["C5"] = (w, dto, reportNo) => w.AfterWash.ToString()!;
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["A3"] = (w, dto, reportNo) => "ISO 16322-2:2021 Method A,Option 1";
                    map["A37"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!;
                    map["S35"] = (w, dto, reportNo) => w.Ballast!;
                }
                if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["A3"] = (w, dto, reportNo) => "ISO 16322-3:2021 Method B";
                    map["A34"] = (w, dto, reportNo) => w.WashingProcedure!;
                    map["L33"] = (w, dto, reportNo) => w.Temperature!;
                    map["X34"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["E35"] = (w, dto, reportNo) => w.Program!.Contains("1:50h") == true ? "1:50h" : w.Program.Contains("1:20h") == true ? "1:20h" : "1h";
                    map["U35"] = (w, dto, reportNo) => w.Program!.Contains("1400") == true ? "1400rpm" : w.Program.Contains("1200") == true ? "1200 rpm" : "600 rpm";
                }
                return map;
            }
        };
        private static readonly Dictionary<string, Func<CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, Dictionary<string, Func<CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>>> PhyExtraMap = new()
        {
            ["Weight"] = (dto, esDto, ws,  reportNo) => new Dictionary<string, Func<CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["J1"] = (dto, esDto, ws,  reportNo) => reportNo,
                ["A3"] = (dto, esDto, ws,  reportNo) => dto.Standard!
            },
            ["Pilling Resistance"] = (dto, esDto, ws,  reportNo) => new Dictionary<string, Func<CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (dto, esDto, ws,  reportNo) => reportNo,
                ["F3"] = (dto, esDto, ws,  reportNo) => dto.Standard!,
                ["D4"] = (dto, esDto, ws,  reportNo) => dto.Parameter!
            },
            ["Extension and Recovery"] = (dto, esDto, ws,  reportNo) =>
            {
                 var map = new Dictionary<string, Func<CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (dto, esDto, ws,  reportNo) => reportNo;
                map["A3"] = (dto, esDto, ws,  reportNo) => dto.Standard!;
                if (dto.sampleDescription!.Contains("Woven"))
                {
                    map["A5"] = (dto, esDto, ws,  reportNo) => dto.sampleDescription!.Contains("Loop") ?
                    "Woven/Non-woven Fabric: method B---Loop trials Perimeter =200mm Speed =100mm/min"
                    : "Woven/Non-woven Fabric: method A---Stripe trials  Guage length=200mm  Speed =200mm/min.";
                }
                else if (dto.sampleDescription!.Contains("Knit"))
                {
                    map["A5"] = (dto, esDto, ws,  reportNo) => dto.sampleDescription!.Contains("Loop") ?
                    "Knitted Fabric: method B---Loop trials  Perimeter =200mm Speed =500mm/min" :
                    "Knitted Fabric: method A---Stripe trials Guage length=100mm Speed =500mm/min.";
                }
                map["F7"] = (dto, esDto, ws,  reportNo) => "3";
                map["N7"] = (dto, esDto, ws,  reportNo) => "5";
                return map;
            },
            ["Abrasion Resistance"] = (dto, esDto, ws,  reportNo) => new Dictionary<string, Func<CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (dto, esDto, ws,  reportNo) => reportNo,
                ["A3"] = (dto, esDto, ws,  reportNo) => dto.Standard!,
                ["C5"] = (dto, esDto, ws,  reportNo) => "9kPa",
                ["I5"] = (dto, esDto, ws,  reportNo) => "30000r"
            },
            ["Snagging Resistance"] = (dto, esDto, ws,  reportNo) => new Dictionary<string, Func<CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (dto, esDto, ws,  reportNo) => reportNo,
                ["J21"] = (dto, esDto, ws,  reportNo) => dto.Standard!,
                ["C23"] = (dto, esDto, ws,  reportNo) => "600"
            },
            ["Tensile Strength"] = (dto, esDto, ws,  reportNo) => new Dictionary<string, Func<CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (dto, esDto, ws,  reportNo) => reportNo,
                ["A28"] = (dto, esDto, ws,  reportNo) => dto.Standard!
            },
            ["Tear Strength"] = (dto, esDto, ws,  reportNo) => new Dictionary<string, Func<CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (dto, esDto, ws,  reportNo) => reportNo,
                ["A3"] = (dto, esDto, ws,  reportNo) => dto.Standard!
            },
            ["Water Repellency-Spray Test"] = (dto, esDto, ws,  reportNo) => new Dictionary<string, Func<CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (dto, esDto, ws,  reportNo) => reportNo,
                ["A3"] = (dto, esDto, ws,  reportNo) => dto.Standard!
            },
            ["Water Resistance-Hydrostatic Pressure"] = (dto, esDto, ws,  reportNo) => new Dictionary<string, Func<CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (dto, esDto, ws,  reportNo) => reportNo,
                ["A3"] = (dto, esDto, ws,  reportNo) => dto.Standard!
            },
            ["Bursting Strength"] = (dto, esDto, ws,  reportNo) =>
            {
                var map = new Dictionary<string, Func<CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (dto, esDto, ws,  reportNo) => esDto.ReportNumber!;
                if (dto.sampleDescription!.Contains("Fabric")) map["I3"] = (dto, esDto, ws,  reportNo) => dto.Standard!;
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["J3"] = (dto, esDto, ws,  reportNo) => dto.Standard!;
                    var sample = ws.Cells["D3"].Value?.ToString();

                    var cellOrder = new List<string> { "A8", "A9", "A10", "A11", "A12", "A13", "A14", "A15", "A16", "A17", "A18", "A19" };
                    var reasonCellOrder = new List<string>();
                    if (dto.Sample!.Contains("Shell") || esDto.SeamParameter!.FirstOrDefault(s => s.Sample == sample)!.Type!.Contains("Shell"))
                    {
                        map["Q4"] = (dto, esDto, ws, reportNo) => "√";
                        reasonCellOrder = cellOrder.Select(c => "J" + c.Substring(1)).ToList();
                    } 
                    else if (dto.Sample.Contains("Lining") || esDto.SeamParameter!.FirstOrDefault(s => s.Sample == sample)!.Type!.Contains("Lining"))
                    {
                        map["AF4"] = (dto, esDto, ws, reportNo) => "√";
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
                                map[cell] = (dto, esDto, ws,  reportNo) => desc;   // 填入对应描述
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
                            map[cell] = (dto, esDto, ws,  reportNo) => desc;
                        }

                        // 2. 当 IsNA == false 时，把 Reason 写到同行 J 列
                        if (info.IsNA == true && !string.IsNullOrWhiteSpace(info.Reason))
                        {
                            string reasonCell = reasonCellOrder[i];
                            string reason = "N/A；" + info.Reason;          // 捕获局部变量
                            map[reasonCell] = (dto, esDto, ws,  reportNo) => reason;
                        }
                    }
                }
                return map;
            },
            ["Seam Slippage"] = (dto, esDto, ws,  reportNo) =>
            {
                var map = new Dictionary<string, Func<CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (dto, esDto, ws,  reportNo) => esDto.ReportNumber!;
                if (dto.sampleDescription!.Contains("Fabric")) map["A3"] = (dto, esDto, ws,  reportNo) => dto.Standard!;
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["J3"] = (dto, esDto, ws,  reportNo) => dto.Standard!;
                    var sample = ws.Cells["D3"].Value?.ToString();

                    var cellOrder = new List<string> { "A5", "A6", "A7", "A8", "A9", "A10", "A11", "A12", "A13", "A14", "A15", "A16" };
                    var reasonCellOrder = new List<string>();
                    if (dto.Sample!.Contains("Shell") || esDto.SeamParameter!.FirstOrDefault(s => s.Sample == sample)!.Type!.Contains("Shell"))
                    {
                        map["Q4"] = (dto, esDto, ws, reportNo) => "√";
                        reasonCellOrder = cellOrder.Select(c => "J" + c.Substring(1)).ToList();
                    }
                    else if (dto.Sample.Contains("Lining") || esDto.SeamParameter!.FirstOrDefault(s => s.Sample == sample)!.Type!.Contains("Lining"))
                    {
                        map["AF4"] = (dto, esDto, ws, reportNo) => "√";
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
                                map[cell] = (dto, esDto, ws,  reportNo) => desc;   // 填入对应描述
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
                            map[cell] = (dto, esDto, ws,  reportNo) => desc;
                        }

                        // 2. 当 IsNA == false 时，把 Reason 写到同行 J 列
                        if (info.IsNA == true && !string.IsNullOrWhiteSpace(info.Reason))
                        {
                            string reasonCell = reasonCellOrder[i];
                            string reason = "N/A；" + info.Reason;         // 捕获局部变量
                            map[reasonCell] = (dto, esDto, ws,  reportNo) => reason;
                        }
                    }
                }
                return map;
            },
            ["Seam Strength"] = (dto, esDto, ws,  reportNo) =>
            {
                var map = new Dictionary<string, Func<CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (dto, esDto, ws,  reportNo) => reportNo;
                if (dto.sampleDescription!.Contains("Garment") && dto.sampleDescription!.Contains("Knit"))
                {
                    map["J5"] = (dto, esDto, ws,  reportNo) => "ISO 13938-2:2019";
                    var sample = ws.Cells["D5"].Value?.ToString();

                    var cellOrder = new List<string> { "A7", "A8", "A9", "A10", "A11", "A12", "A13", "A14", "A15", "A16", "A17", "A18" };
                    var reasonCellOrder = new List<string>();
                    if (dto.Sample!.Contains("Shell") || esDto.SeamParameter!.FirstOrDefault(s => s.Sample == sample)!.Type!.Contains("Shell"))
                    {
                        map["Q6"] = (dto, esDto, ws, reportNo) => "√";
                        reasonCellOrder = cellOrder.Select(c => "J" + c.Substring(1)).ToList();
                    }
                    else if (dto.Sample.Contains("Lining") || esDto.SeamParameter!.FirstOrDefault(s => s.Sample == sample)!.Type!.Contains("Lining"))
                    {
                        map["AF6"] = (dto, esDto, ws, reportNo) => "√";
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
                                map[cell] = (dto, esDto, ws,  reportNo) => desc;   // 填入对应描述
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
                            map[cell] = (dto, esDto, ws,  reportNo) => desc;
                        }

                        // 2. 当 IsNA == false 时，把 Reason 写到同行 J 列
                        if (info.IsNA == true && !string.IsNullOrWhiteSpace(info.Reason))
                        {
                            string reasonCell = reasonCellOrder[i];
                            string reason = "N/A；" + info.Reason;         // 捕获局部变量
                            map[reasonCell] = (dto, esDto, ws,  reportNo) => reason;
                        }
                    }
                }
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["J18"] = (dto, esDto, ws,  reportNo) => dto.Standard!;
                    var sample = ws.Cells["D3"].Value?.ToString();
                    var cellOrder = new List<string> { "A20", "A21", "A22", "A23", "A24", "A25", "A26", "A27", "A28", "A29", "A30", "A31" };
                    var reasonCellOrder = new List<string>();
                    if (dto.Sample!.Contains("Shell") || esDto.SeamParameter!.FirstOrDefault(s => s.Sample == sample)!.Type!.Contains("Shell"))
                    {
                        map["Q19"] = (dto, esDto, ws, reportNo) => "√";
                        reasonCellOrder = cellOrder.Select(c => "J" + c.Substring(1)).ToList();
                    }
                    else if (dto.Sample.Contains("Lining") || esDto.SeamParameter!.FirstOrDefault(s => s.Sample == sample)!.Type!.Contains("Lining"))
                    {
                        map["AF19"] = (dto, esDto, ws, reportNo) => "√";
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
                                map[cell] = (dto, esDto, ws,  reportNo) => desc;   // 填入对应描述
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
                            map[cell] = (dto, esDto, ws,  reportNo) => desc;
                        }

                        // 2. 当 IsNA == false 时，把 Reason 写到同行 J 列
                        if (info.IsNA == true && !string.IsNullOrWhiteSpace(info.Reason))
                        {
                            string reasonCell = reasonCellOrder[i];
                            string reason = "N/A；" + info.Reason;          // 捕获局部变量
                            map[reasonCell] = (dto, esDto, ws,  reportNo) => reason;
                        }
                    }
                }
                return map;
            },
        };



        private static readonly Dictionary<string, int> OffsetRule = new()
        {
            ["DS to Dry-clean"] = 4,
            ["Water Repellency-Spray Test"] = 3,
            ["Heat Press Test For JAKO"] = 4,
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
            string SampleDescription)
        {
            int offset = OffsetRule.GetValueOrDefault(itemName, 0);

            if (afmap != null && afmap.Length > 0 && itemName == "Spirality/Skewing")
            {
                for (int i = 0; i < afmap.Length; i++)
                {
                    ws.Cells[AfterWashCellAddrs![i]].Value = afmap[i];
                }
            }
            else if (afmap != null && afmap.Length > 0 && (itemName == "Appearance"&& SampleDescription.Contains("Garment")))
            {
                for (int i = 0; i < AfterWashCellAddrs!.Length; i++)
                {
                    ws.Cells[AfterWashCellAddrs![i]].Value = afmap[0];
                }
            }

            if (itemName == "Appearance" || itemName == "Print Durability For JAKO")
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
