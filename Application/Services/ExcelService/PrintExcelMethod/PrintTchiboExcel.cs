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

namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.PrintExcelMethod
{

    public sealed class PrintTchiboExcel : IPrintExcelStrategy
    {
        private readonly LabDbContextSec _db;
        public PrintTchiboExcel(LabDbContextSec db)
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
            string[]? AfterWashCellAddrs = null;
            if (itemName == "DS to Washing" || itemName == "DS to Dry-clean" || itemName == "Appearance" || itemName == "Spriality/Skewing")
            {
                AfterWashCellAddrs = AfterWashCellMapper[itemName](itemName, dto.sampleDescription!);
            }


            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            var samples = dto.Sample!.Split(',').Select(s => s.Trim()).ToArray();
            int[]? afterWashMap = null;
            if (itemName == "DS to Washing" || itemName == "DS to Dry-clean" || itemName == "Appearance" || itemName == "Spriality/Skewing")
            {
                var wp = _db.WetParameterIsos
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                if (wp == null) wp = new WetParameterIso();
                string? afterWash = wp!.AfterWash;
                string? iron = wp!.Iron;
                samples = SampleNumCounter.GetSample(dto.Sample!, afterWash, iron);
                afterWashMap = SampleNumCounter.ExpandWashNumbers(samples!, afterWash!,iron);
            }
            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            int offset = 0; // 假设没有偏移
            offset = OffsetRule.GetValueOrDefault(itemName, 0);
            int capacity = offset > 0 ? cellAddrs.Length / 2 : cellAddrs.Length; // 根据是否偏移计算每张 Sheet 的实际容量
            if (itemName == "CF to Hot Pressing") { capacity = 3; }// 特例处理，实际容量为3
            if (itemName == "Appearance") { capacity = 1; }
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
                WriteSamples(ws, slice, afmap, cellAddrs, AfterWashCellAddrs, itemName,dto.sampleDescription);
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
                    var extraMap = PhyExtraMap.GetValueOrDefault(itemName, (wp,dto, reportNo) => new Dictionary<string, Func<WetParameterIso,CheckListDto, string, string>>())(wp,dto, reportNo);
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
            ["Weight"] = "Weight",
            ["Yarn Count"] = "Yarn Count",
            ["Pilling Resistance"] = "Pilling Resistance",
            ["Zipper Strength"] = "Zipper Strength-EN 16732",
            ["Resistance to Unsnapping of Snap Fasteners"] = "Resistance to Unsnapping",
            ["Water Resistance-Hydrostatic Pressure"] = "Hydroatatic Test",
            ["Extension and Recovery"] = "Stretch&Recovery of Elastic",
            ["Air Permeability"] = "Air Permeability",
            ["Absorbency"] = "Absorbency",
            ["Attachment Strength"]= "Attachment Strength",
            ["Density"] = "Density",
            ["Appearance"] = "AppearanceAfterWashing",
            ["CF to Washing"] = "CFtoWashing&Rubbing&Light",
            ["CF to Rubbing"] = "CFtoWashing&Rubbing&Light",
            ["CF to Light"] = "CFtoWashing&Rubbing&Light",
            ["CF to Perspiration"] = "CFtoPerspiration&Water",
            ["CF to Water"] = "CFtoPerspiration&Water",
            ["CF to Saliva"] = "CFtoSaliva&Sweat",
            ["CF to Sweat"] = "CFtoSaliva&Sweat",
            ["CF to Sublimation in Storage"] = "CFtoSublimation&HotPressing&Cl",
            ["CF to Hot Pressing"] = "CFtoSublimation&HotPressing&Cl",
            ["CF to Chlorinated Water"] = "CFtoSublimation&HotPressing&Cl",
        };
        private static readonly Dictionary<string, Dictionary<string, string>> TemplateSheetNames = new()
        {
            ["DS to Washing"] = new Dictionary<string, string>
            {
                {"Fabric", "DStoWashing-F" },
                {"Garment","DStoWashing-G"},
            },
            ["Seam Slippage"] = new Dictionary<string, string>
            {
                {"Fabric", "Seam Slippage" },
                {"Garment","Seam Slippage-G"},
            },
        };
        private static readonly Dictionary<string, Func<string, string, string[]>> CellMapper = new()
        {
            ["Appearance"] = (_, _) => ExcelTchiboMapper.MapAppearance(),
            ["Weight"] = (_, _) => ExcelTchiboMapper.MapWeight(),
            ["Yarn Count"] = (_, _) => ExcelTchiboMapper.MapYarnCount(),
            ["Pilling Resistance"] = (_, _) => ExcelTchiboMapper.MapPilling(),
            ["Zipper Strength"] = (_, _) => ExcelTchiboMapper.MapZipperStrength(),
            ["Resistance to Unsnapping of Snap Fasteners"] = (_, _) => ExcelTchiboMapper.MapUnsnapping(),
            ["Water Resistance-Hydrostatic Pressure"] = (_, _) => ExcelTchiboMapper.MapHydrostaticPressing(),
            ["Extension and Recovery"] = (_, _) => ExcelTchiboMapper.MapExtensionAndRecovery(),
            ["Air Permeability"] = (_, _) => ExcelTchiboMapper.MapAirPermeability(),
            ["Absorbency"] = (_, _) => ExcelTchiboMapper.MapAbsorbency(),
            ["Attachment Strength"] = (_, _) => ExcelTchiboMapper.MapAttachmentStrength(),
            ["Density"] = (_, _) => ExcelTchiboMapper.MapDensity(),
            ["CF to Washing"] = (_, _) => ExcelTchiboMapper.MapCFtoWashing(),
            ["CF to Rubbing"] = (_, _) => ExcelTchiboMapper.MapCFtoRubbing(),
            ["CF to Light"] = (_, _) => ExcelTchiboMapper.MapCFtoLight(),
            ["CF to Perspiration"] = (_, _) => ExcelTchiboMapper.MapCFtoPerspiration(),
            ["CF to Water"] = (_, _) => ExcelTchiboMapper.MapCFtoWater(),
            ["CF to Saliva"] = (_, _) => ExcelTchiboMapper.MapCFtoSalivaSweat(),
            ["CF to Sweat"] = (_, _) => ExcelTchiboMapper.MapCFtoSalivaSweat(),
            ["CF to Sublimation in Storage"] = (_, _) => ExcelTchiboMapper.MapCFtoSublimation(),
            ["CF to Hot Pressing"] = (_, _) => ExcelTchiboMapper.MapCFtoHotPressing(),
            ["CF to Chlorinated Water"] = (_, _) => ExcelTchiboMapper.MapCFtoCl(),
            ["DS to Washing"] = (_, m) => ExcelTchiboMapper.MapDStoWashing(m),
            ["Seam Slippage"] = (_, m) => ExcelTchiboMapper.MapSeamSlippage(m)
        };
        //取洗涤遍数映射地址的函数
        private static readonly Dictionary<string, Func<string, string, string[]>> AfterWashCellMapper = new()
        {
            ["DS to Washing"] = (_, m) => ExcelTchiboMapper.DStoWashingAf(m),
            ["Appearance"] = (_, _) => ExcelTchiboMapper.AppearanceAf(),
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
                       map["BA4"] = (w, dto, reportNo) => w.Temperature!;
                       map["BH4"] = (w, dto, reportNo) => w.Detergent!;
                       map["AV5"] = (w, dto, reportNo) => w.WashingProcedure!;
                       map["BP5"] = (w, dto, reportNo) => w.DryProcedure!;
                       map["AR6"] = (w, dto, reportNo) => w.SpecialCareInstruction ?? null;
                       map["BJ6"] = (w, dto, reportNo) => w.Program!;
                   }
                   else if (dto.sampleDescription!.Contains("Garment"))
                   {
                       map["P1"] = (w, dto, reportNo) => reportNo;
                       map["A3"] = (w, dto, reportNo) => dto.Standard!;
                       map["L4"] = (w, dto, reportNo) => w.Temperature!;
                       map["S4"] = (w, dto, reportNo) => w.Detergent!;
                       map["A5"] = (w, dto, reportNo) => w.WashingProcedure!;
                       map["Y5"] = (w, dto, reportNo) => w.DryProcedure!;
                       map["A6"] = (w, dto, reportNo) => w.SpecialCareInstruction ?? null;
                       map["W6"] = (w, dto, reportNo) => w.Program!;
                   }
                   return map;
               },
            ["Appearance"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR4"] = (w, dto, reportNo) => dto.Standard!,
                ["BI13"] = (w, dto, reportNo) => w.IronMethod??"/",
                ["BA38"] = (w, dto, reportNo) => w.Temperature!,
                ["BH38"] = (w, dto, reportNo) => w.Detergent!,
                ["AV39"] = (w, dto, reportNo) => w.WashingProcedure!,
                ["BT39"] = (w, dto, reportNo) => w.DryProcedure!,
                ["AR40"] = (w, dto, reportNo) => w.SpecialCareInstruction ?? null,
            },
            ["CF to Sublimation in Storage"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["H1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["D4"] = (w, dto, reportNo) => w.Temperature!,
                ["G4"] = (w, dto, reportNo) => "48",
                ["D7"] = (w, dto, reportNo) => w.Ballast!
            },
            ["CF to Hot Pressing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["H1"] = (w, dto, reportNo) => reportNo,
                ["A12"] = (w, dto, reportNo) => dto.Standard!,
                ["G13"] = (w, dto, reportNo) => w.Temperature!,
                ["A14"] = (w, dto, reportNo) => w.Iron=="L-5"?"该项目号最高可给5级":null!,
                ["R13"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.IronMethod) ? "N/A" : null,
            },
            ["CF to Chlorinated Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["H1"] = (w, dto, reportNo) => reportNo,
                ["A27"] = (w, dto, reportNo) => dto.Standard!
            },
            ["CF to Washing"]= (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>> 
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
            ["CF to Light"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A28"] = (w, dto, reportNo) => dto.Standard!,
                ["B31"] = (w, dto, reportNo) => dto.Parameter!,
            },
            ["CF to Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A25"] = (w, dto, reportNo) => dto.Standard!,
                ["F25"] = (w, dto, reportNo) => dto.Parameter=="L-5" ? "该项目号最高可给5级" : null!,
            },
            ["CF to Perspiration"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["CF to Saliva"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["H15"] = (w, dto, reportNo) => dto.Parameter == "L-5" ? "该项目号最高可给5级" : null!,
                ["G3"] = (w, dto, reportNo) => "√"
            },
            ["CF to Sweat"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["H15"] = (w, dto, reportNo) => dto.Parameter == "L-5" ? "该项目号最高可给5级" : null!,
                ["J3"] = (w, dto, reportNo) => "√"
            },
        };
        private static readonly Dictionary<string, Func<WetParameterIso,CheckListDto, string, Dictionary<string, Func<WetParameterIso,CheckListDto, string, string>>>> PhyExtraMap = new()
        {
            ["Weight"]= (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["J1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Yarn Count"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Pilling Resistance"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["F3"] = (w, dto, reportNo) => dto.Standard!,
                ["D4"] = (w, dto, reportNo) => dto.Parameter!,
            },
            ["Zipper Strength"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Resistance to Unsnapping of Snap Fasteners"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Extension and Recovery"] = (w, dto, reportNo) => 
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (wp, dto, reportNo) => reportNo;
                map["A3"] = (wp, dto, reportNo) => dto.Standard!;
                if (dto.sampleDescription!.Contains("Woven"))
                {
                    map["F7"] = (wp, dto, reportNo) => "30";
                    map["A5"] = (wp, dto, reportNo) => dto.sampleDescription!.Contains("Loop") ?
                    "Woven/Non-woven Fabric: method B---Loop trials Perimeter =200mm Speed =100mm/min" 
                    : "Woven/Non-woven Fabric: method A---Stripe trials  Guage length=200mm  Speed =200mm/min.";
                }
                else if (dto.sampleDescription!.Contains("Knit")) 
                {
                    map["A5"] = (wp, dto, reportNo) => dto.sampleDescription!.Contains("Loop") ?
                    "Knitted Fabric: method B---Loop trials  Perimeter =200mm Speed =500mm/min" :
                    "Knitted Fabric: method A---Stripe trials Guage length=100mm Speed =500mm/min.";
                    map["F7"] = (wp, dto, reportNo) =>
                    dto.sampleDescription!.Contains("3")?"3"
                    : dto.sampleDescription!.Contains("4") ? "4"
                    : dto.sampleDescription!.Contains("5") ? "5"
                    : dto.sampleDescription!.Contains("6") ? "6"
                    : dto.sampleDescription!.Contains("7") ? "7"
                    : dto.sampleDescription!.Contains("8") ? "8"
                    : dto.sampleDescription!.Contains("10") ? "10"
                    :"14";
                }
                map["L7"] = (wp, dto, reportNo) => "5";
                return map;
            },
            ["Water Resistance-Hydrostatic Pressure"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (wp, dto, reportNo) => reportNo,
                ["A3"] = (wp, dto, reportNo) => dto.Standard!,
            },
            ["Seam Slippage"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso,CheckListDto, string, string>>();
                map["M1"] = (wp,dto, reportNo) => reportNo;
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["A3"] = (wp, dto, reportNo) => dto.Standard!;
                }
                if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["J3"] = (wp,dto, reportNo) => dto.Standard!;
                    string? layout = SeamExtraHelper.GetExtraField<string>(dto, "layout", objIndex: 0);
                    if (layout.Contains("Shell"))
                    {
                        map["Q4"] = (wp, dto, reportNo) => "√";
                        map["Q14"] = (wp,dto, reportNo) => "√";
                    }
                    if (layout.Contains("Lining"))
                    {
                        map["AF4"] = (wp, dto, reportNo) => "√";
                        map["AF14"] = (wp,dto, reportNo) => "√";
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
                                map[cell] = (wp, dto, reportNo) => desc;
                            }
                        }
                    }
                }
                return map;
            },
            ["Air Permeability"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (wp, dto, reportNo) => reportNo,
                ["A3"] = (wp, dto, reportNo) => dto.Standard!,
                ["F5"] = (wp, dto, reportNo) => "20",
                ["E6"] = (wp, dto, reportNo) => "100",
            },
            ["Absorbency"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (wp, dto, reportNo) => reportNo,
                ["A3"] = (wp, dto, reportNo) => dto.Standard!,
                ["A20"] = (wp, dto, reportNo) => "ISO 5077:2007 / ISO 3759:2011 / ISO 6330:2021",
                ["G21"] = (wp, dto, reportNo) => w.WashingProcedure!,
                ["AK21"] = (wp, dto, reportNo) => w.Temperature!,
                ["Q22"] = (wp, dto, reportNo) => w.Ballast!,
                ["S23"] = (wp, dto, reportNo) => w.DryProcedure!,
                ["A24"] = (wp, dto, reportNo) => w.SpecialCareInstruction??null!
            },
            ["Attachment Strength"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (wp, dto, reportNo) => reportNo,
                ["A3"] = (wp, dto, reportNo) => dto.Standard!,
                ["A17"] = (wp, dto, reportNo) => dto.Standard!,
            },
            ["Density"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (wp, dto, reportNo) => reportNo,
                ["A3"] = (wp, dto, reportNo) => dto.Standard!,
            },
        };



        private static readonly Dictionary<string, int> OffsetRule = new()
        {
            ["CF to Perspiration"] = 6,
            ["DS to Washing"] = 4,
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
            if (afmap != null && afmap.Length > 0 && AfterWashCellAddrs != null && AfterWashCellAddrs.Length > 0 && itemName == "Appearance")
            {
                for (int i = 0; i < AfterWashCellAddrs.Length; i++)
                {
                    ws.Cells[AfterWashCellAddrs![i]].Value = afmap[0];
                }
            }
            else if (afmap != null && afmap.Length > 0 && itemName == "DS to Washing" && sampleDescription.Contains("Garment"))
            {
                for (int i = 0; i < afmap.Length; i++)
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



            if (itemName == "Appearance")
            {
                for (int i = 0; i < cellAddrs.Length; i++)
                {
                    ws.Cells[cellAddrs[i]].Value = slice[0];
                }
            }
            else if (itemName == "CF to Hot Pressing") 
            {
                for (int i = 0; i < slice.Length; i++)
                {
                    ws.Cells[cellAddrs[i]].Value = slice[i];
                    ws.Cells[cellAddrs[i+3]].Value = slice[i];
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