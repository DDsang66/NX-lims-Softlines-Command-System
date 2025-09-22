using OfficeOpenXml;
using static NX_lims_Softlines_Command_System.Application.Services.Factory.PrintExcelStrategyFactory;
using System.ComponentModel;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelMapper;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.Helper;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;

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
            foreach (var row in selectedRows!)
            {
                checkLists.Add(new CheckListDto
                {
                    ItemName = row.itemName,
                    Standard = row.standards,
                    Parameter = row.parameters,
                    Type = row.types,
                    Sample = row.samples,
                    Extra = row.extra,
                    MenuName = menu,
                    sampleDescription = sampleDescription,
                });
            }
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
            //<-------------------------------------------------------------------------------------->
            string? tplName = null;
            bool foundInSub = false;
            // 1) 模板 sheet
            if (TemplateSheetNames.TryGetValue(itemName, out var subDictionary))
            {
                /* ---------- 只有 Seam Strength 需要多条件 ---------- */
                if (itemName == "Seam Strength")
                {
                    bool hasGarment = dto.sampleDescription.Contains("Garment", StringComparison.OrdinalIgnoreCase);
                    bool hasKnit = dto.sampleDescription.Contains("Knit", StringComparison.OrdinalIgnoreCase);

                    // 按优先级命中
                    if (hasKnit && hasGarment &&
                        subDictionary.TryGetValue("Knit", out tplName))   // 字典里放的是 Seam Bursting-G
                    {
                        foundInSub = true;
                    }
                    else if (hasGarment &&
                             subDictionary.TryGetValue("Garment", out tplName)) // Seam Slippage&Breakage-G
                    {
                        foundInSub = true;
                    }
                    /* 只命中 Knit 就不管，保持 foundInSub == false */
                }
                else
                {
                    /* ---------- 其余测试保持原单关键字逻辑 ---------- */
                    foreach (var kvp in subDictionary)
                    {
                        if (string.IsNullOrEmpty(kvp.Key) ||
                            dto.sampleDescription!.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            tplName = kvp.Value;
                            foundInSub = true;
                            break;
                        }
                    }
                }
            }
            //如果在 TemplateSheetNames 中未找到，尝试从 TemplateSheetNamesNormal 中查找
            if (!foundInSub)
            {
                TemplateSheetNamesNormal.TryGetValue(itemName, out tplName);
            }

            // 如果仍未找到匹配的模板名
            if (tplName == null)
            {
                Console.WriteLine("未找到对应的模板名");
                tplName = "DefaultSheetName"; // 假设有一个默认模板名
            }
            var template = pkg.Workbook.Worksheets[tplName];
            //<-------------------------------------------------------------------------------------->

            // 2) 计算需要几张 sheet
            var cellAddrs = CellMapper[itemName](itemName, dto.sampleDescription!);
            var samples = dto.Sample!.Split(',').Select(s => s.Trim()).ToArray();
            int offset = 0;
            if (dto.sampleDescription!.Contains("Fabric"))
            {
                offset = OffsetRule.GetValueOrDefault(itemName, 0);
            }// 获取偏移量，默认为0
            int capacity = offset > 0 ? cellAddrs.Length / 2 : cellAddrs.Length; // 根据是否偏移计算每张 Sheet 的实际容量
            int sheetCnt = (int)Math.Ceiling(samples.Length / (double)capacity);


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

                /* 把这段样本写进去 */
                WriteSamples(ws, slice, cellAddrs, itemName, dto.sampleDescription!);

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
                    var extraMap = PhyExtraMap.GetValueOrDefault(itemName, (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>())(dto, reportNo);
                    foreach (var kv in extraMap)
                    {
                        ws.Cells[kv.Key].Value = kv.Value(dto, reportNo);
                    }
                }
            }


        }
        private static readonly Dictionary<string, string> TemplateSheetNamesNormal = new()
        {
            ["CF to Washing"] = "CFtoWashing&Rubbing&Light",
            ["CF to Rubbing"] = "CFtoWashing&Rubbing&Light",
            ["CF to Light"] = "CFtoWashing&Rubbing&Light",
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
        private static readonly Dictionary<string, Dictionary<string, string>> TemplateSheetNames = new()
        {
            ["Appearance"] = new Dictionary<string, string>
            {
                {"Fabric", "AppearanceAfterWashing-F" },
                {"Garment","AppearanceAfterWashing-G"},
            },
            ["DS to Dry-clean"] = new Dictionary<string, string>
            {
                {"Fabric", "DStoDryclean-F" },
                {"Garment", "DStoDryclean-G" },
                {"Socks", "DStoDryclean-Acc" },
                {"Gloves", "DStoDryclean-Acc" },
                {"Cap", "DStoDryclean-Acc" },
            },
            ["Spriality/Skewing"] = new Dictionary<string, string>
            {
                {"Fabric", "Spirality-F" },
                {"Garment", "Spirality-G" },
            },
            ["Seam Slippage"] = new Dictionary<string, string>
            {
                {"Fabric", "Seam Slippage&Tensile" },
                {"Garment", "Seam Slippage&Breakage-G" },
            },
            ["Bursting Strength"] = new Dictionary<string, string>
            {
                 {"Fabric","Bursting Strength"},
                 {"Knit","Seam Bursting-G"}
            },
            ["Seam Strength"] = new Dictionary<string, string>
            {
                 {"Garment","Seam Slippage&Breakage-G"},
                 {"Knit","Seam Bursting-G"}
            },
            ["Zipper Strength"] = new Dictionary<string, string>
            {
                 {"EN","Zipper Strength-ASTM D2061"},
                 {"ASTM","Zipper Strength-EN 16732"}
            }
        };
        private static readonly Dictionary<string, Func<string, string, string[]>> CellMapper = new()
        {
            ["Appearance"] = (_, m) => ExcelJakoMapper.MapAppearance(m),
            ["DS to Dry-clean"] = (_, m) => ExcelJakoMapper.MapDStoDS(m),
            ["CF to Washing"] = (n, _) => ExcelJakoMapper.MapWRL(n),
            ["CF to Rubbing"] = (n, _) => ExcelJakoMapper.MapWRL(n),
            ["CF to Light"] = (n, _) => ExcelJakoMapper.MapWRL(n),
            ["CF to Perspiration"] = (n, _) => ExcelJakoMapper.MapPWD(n),
            ["CF to Water"] = (n, _) => ExcelJakoMapper.MapPWD(n),
            ["CF to Dry-clean"] = (n, _) => ExcelJakoMapper.MapPWD(n),
            ["Spriality/Skewing"] = (_, m) => ExcelJakoMapper.MapSpirality(m),
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
                    map["AR12"] = (w, dto, reportNo) => w.SpecialCareInstruction ?? null;
                    map["BF6"] = (w, dto, reportNo) => w.Ballast!;
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
            },
            ["CF to Hot Pressing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A12"] = (w, dto, reportNo) => dto.Standard!,
                ["E13"] = (w, dto, reportNo) => w.Temperature!,
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
                ["D33"] = (w, dto, reportNo) => w.Temperature!,
                ["G33"] = (w, dto, reportNo) => dto.Sample!
            },
            ["Spriality/Skewing"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["P1"] = (w, dto, reportNo) => reportNo;
                map["C5"] = (w, dto, reportNo) => w.AfterWash.ToString()!;
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["A3"] = (w, dto, reportNo) => "ISO 16322-2:2021 Method A,Option 1";
                    map["A37"] = (w, dto, reportNo) => w.SpecialCareInstruction ?? null;
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
        private static readonly Dictionary<string, Func<CheckListDto, string, Dictionary<string, Func<CheckListDto, string, string>>>> PhyExtraMap = new()
        {
            ["Weight"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["J1"] = (dto, reportNo) => reportNo,
                ["A3"] = (dto, reportNo) => dto.Standard!
            },
            ["Pilling Resistance"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["M1"] = (dto, reportNo) => reportNo,
                ["F3"] = (dto, reportNo) => dto.Standard!,
                ["D4"] = (dto, reportNo) => dto.Parameter!
            },
            ["Extension and Recovery"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["M1"] = (dto, reportNo) => reportNo,
                ["A3"] = (dto, reportNo) => dto.Standard!,
                ["F7"] = (dto, reportNo) => "3",
                ["N7"] = (dto, reportNo) => "5"
            },
            ["Abrasion Resistance"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["M1"] = (dto, reportNo) => reportNo,
                ["A3"] = (dto, reportNo) => dto.Standard!,
                ["C5"] = (dto, reportNo) => "9kPa",
                ["I5"] = (dto, reportNo) => "30000r"
            },
            ["Snagging Resistance"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["M1"] = (dto, reportNo) => reportNo,
                ["J21"] = (dto, reportNo) => dto.Standard!,
                ["C23"] = (dto, reportNo) => "600"
            },
            ["Seam Slippage"] = (dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<CheckListDto, string, string>>();
                map["M1"] = (dto, reportNo) => reportNo;
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["A3"] = (dto, reportNo) => dto.Standard!;
                }
                if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["J3"] = (dto, reportNo) => dto.Standard!;
                    string? layout = SeamExtraHelper.GetExtraField<string>(dto, "layout", objIndex: 0);
                    if (layout.Contains("Shell"))
                    {
                        map["Q4"] = (dto, reportNo) => "√";
                        map["Q14"] = (dto, reportNo) => "√";
                    }
                    if (layout.Contains("Lining"))
                    {
                        map["AF4"] = (dto, reportNo) => "√";
                        map["AF14"] = (dto, reportNo) => "√";
                    }
                    string? component = SeamExtraHelper.GetExtraField<string>(dto, "component", objIndex: 0);
                    Dictionary<string, (string Cell, string Desc)> ComponentMap =
                            new(StringComparer.OrdinalIgnoreCase)
                            {
                                ["Side"] = ("A5", "Side Seam"),
                                ["Sleeve"] = ("A6", "Sleeve Seam"),
                                ["Armhole"] = ("A7", "Armhole Seam"),
                                ["Shoulder"] = ("A8", "Shoulder Seam"),
                                ["Armprit"] = ("A9", "Armprit Seam"),
                                ["Front Panel"] = ("A10", "Front Panel Seam"),
                                ["Back Panel"] = ("A11", "Back Panel Seam"),
                                ["OutSide"] = ("A15", "Out-Side Seam"),
                                ["InSide"] = ("A16", "In-Side Seam"),
                                ["Back Rise"] = ("A17", "Back Rise Seam"),
                                ["Front Crotch"] = ("A18", "Front Crotch Seam"),
                                ["Cross"] = ("A19", "Cross Seam"),
                            };
                    if (!string.IsNullOrEmpty(component))
                    {
                        foreach (var kv in ComponentMap)
                        {
                            if (component.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                            {
                                var (cell, desc) = kv.Value;
                                map[cell] = (dto, reportNo) => desc;
                            }
                        }
                    }
                }
                return map;
            },
            ["Seam Strength"] = (dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<CheckListDto, string, string>>();
                map["M1"] = (dto, reportNo) => reportNo;
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["A3"] = (dto, reportNo) => dto.Standard!;
                }
                if (dto.sampleDescription!.Contains("Garment"))
                {
                    string? component = SeamExtraHelper.GetExtraField<string>(dto, "component", objIndex: 0);
                    string? layout = SeamExtraHelper.GetExtraField<string>(dto, "layout", objIndex: 0);
                    if (dto.sampleDescription!.Contains("Knit"))
                    {
                        map["A3"] = (dto, reportNo) => "DIN EN ISO 13938-2:2020";
                        if (layout.Contains("Shell") && !string.IsNullOrEmpty(layout))
                        {
                            map["Q5"] = (dto, reportNo) => "√";
                            map["Q15"] = (dto, reportNo) => "√";
                        }
                        if (layout.Contains("Lining") && !string.IsNullOrEmpty(layout))
                        {
                            map["AF5"] = (dto, reportNo) => "√";
                            map["AF15"] = (dto, reportNo) => "√";
                        }
                        Dictionary<string, (string Cell, string Desc)> ComponentMap =
                            new(StringComparer.OrdinalIgnoreCase)
                            {
                                ["Side"] = ("A6", "Side Seam"),
                                ["Sleeve"] = ("A7", "Sleeve Seam"),
                                ["Armhole"] = ("A8", "Armhole Seam"),
                                ["Shoulder"] = ("A9", "Shoulder Seam"),
                                ["Armprit"] = ("A10", "Armprit Seam"),
                                ["Front Panel"] = ("A11", "Front Panel Seam"),
                                ["Back Panel"] = ("A12", "Back Panel Seam"),
                                ["OutSide"] = ("A16", "Out-Side Seam"),
                                ["InSide"] = ("A17", "In-Side Seam"),
                                ["Back Rise"] = ("A18", "Back Rise Seam"),
                                ["Front Crotch"] = ("A19", "Front Crotch Seam"),
                                ["Cross"] = ("A20", "Cross Seam"),
                            };
                        if (!string.IsNullOrEmpty(component))
                        {
                            foreach (var kv in ComponentMap)
                            {
                                if (component.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                                {
                                    var (cell, desc) = kv.Value;
                                    map[cell] = (dto, reportNo) => desc;
                                }
                            }
                        }

                    }
                    else
                    {
                        map["J22"] = (dto, reportNo) => dto.Standard!;
                        if (layout.Contains("Shell") && !string.IsNullOrEmpty(layout))
                        {
                            map["Q23"] = (dto, reportNo) => "√";
                            map["Q33"] = (dto, reportNo) => "√";
                        }
                        if (layout.Contains("Lining") && !string.IsNullOrEmpty(layout))
                        {
                            map["AF23"] = (dto, reportNo) => "√";
                            map["AF33"] = (dto, reportNo) => "√";
                        }
                        Dictionary<string, (string Cell, string Desc)> ComponentMap =
                            new(StringComparer.OrdinalIgnoreCase)
                            {
                                ["Side"] = ("A24", "Side Seam"),
                                ["Sleeve"] = ("A25", "Sleeve Seam"),
                                ["Armhole"] = ("A26", "Armhole Seam"),
                                ["Shoulder"] = ("A27", "Shoulder Seam"),
                                ["Armprit"] = ("A28", "Armprit Seam"),
                                ["Front Panel"] = ("A29", "Front Panel Seam"),
                                ["Back Panel"] = ("A30", "Back Panel Seam"),
                                ["OutSide"] = ("A34", "Out-Side Seam"),
                                ["InSide"] = ("A35", "In-Side Seam"),
                                ["Back Rise"] = ("A36", "Back Rise Seam"),
                                ["Front Crotch"] = ("A37", "Front Crotch Seam"),
                                ["Cross"] = ("A38", "Cross Seam"),
                            };
                        if (!string.IsNullOrEmpty(component))
                        {
                            foreach (var kv in ComponentMap)
                            {
                                if (component.Contains(kv.Key, StringComparison.OrdinalIgnoreCase))
                                {
                                    var (cell, desc) = kv.Value;
                                    map[cell] = (dto, reportNo) => desc;
                                }
                            }
                        }
                    }


                }
                return map;
            },
            ["Bursting Strength"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["M1"] = (dto, reportNo) => reportNo,
                ["I3"] = (dto, reportNo) => dto.Standard!
            },
            ["Tensile Strength"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["M1"] = (dto, reportNo) => reportNo,
                ["A28"] = (dto, reportNo) => dto.Standard!
            },
            ["Tear Strength"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["M1"] = (dto, reportNo) => reportNo,
                ["A3"] = (dto, reportNo) => dto.Standard!
            },
            ["Water Repellency-Spray Test"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["M1"] = (dto, reportNo) => reportNo,
                ["A3"] = (dto, reportNo) => dto.Standard!
            },
            ["Water Resistance-Hydrostatic Pressure"] = (dto, reportNo) => new Dictionary<string, Func<CheckListDto, string, string>>
            {
                ["M1"] = (dto, reportNo) => reportNo,
                ["A3"] = (dto, reportNo) => dto.Standard!
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
            string[] cellAddrs,
            string itemName,
            string sampleDescription)
        {
            int offset = OffsetRule.GetValueOrDefault(itemName, 0);
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
