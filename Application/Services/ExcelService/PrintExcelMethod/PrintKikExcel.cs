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
    public sealed class PrintKikExcel : IPrintExcelStrategy
    {
        private readonly LabDbContextSec _db;
        public PrintKikExcel(LabDbContextSec db)
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
            if (itemName == "DS to Washing" || itemName == "DS to Dry-clean" || itemName == "Appearance")
            {
                AfterWashCellAddrs = AfterWashCellMapper[itemName](itemName, dto.sampleDescription!);
            }

            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            var samples = dto.Sample!.Split(',').Select(s => s.Trim()).ToArray();
            int[]? afterWashMap = null;
            if (itemName == "DS to Washing")
            {
                var wp = _db.WetParameterIsos
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                if (wp == null) wp = new WetParameterIso();
                string? afterWash = wp!.AfterWash;

                afterWash = string.Join(", ", dto.Sample!
                    .Split(',')
                    .Select(s => s.Trim())
                    .SelectMany(s => new[] { $"{s}-1 Wash" }));
                string? iron = wp!.Iron;
                string? ironMethod = wp!.IronMethod;
                samples = SampleNumCounter.GetSample(dto.Sample!, afterWash, iron, ironMethod);
                afterWashMap = SampleNumCounter.ExpandWashNumbers(samples!, afterWash!, iron);
            }
            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            int offset = 0; // 假设没有偏移
            offset = OffsetRule.GetValueOrDefault(itemName, 0);
            int capacity = offset > 0 ? cellAddrs.Length / 2 : cellAddrs.Length; // 根据是否偏移计算每张 Sheet 的实际容量
            if (itemName == "Appearance") { capacity = 1; }
            if (itemName == "DS to Washing" && !dto.sampleDescription!.Contains("Fabric")) capacity = 1;
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
            ["Weight"] = "Weight",
            ["Piece Weight"] = "Weight",
            ["Yarn Count"] = "Yarn Count",
            ["Pilling Resistance"] = "Pilling Resistance",
            ["Zipper Strength"] = "Zipper Strength",
            ["Water Resistance-Hydrostatic Pressure"] = "Hydroatatic",
            ["Air Permeability"] = "Air Permeability",
            ["Attachment Strength"] = "Attachment Strength",
            ["Density"] = "Density",

            ["Spirality/Skewing"] = "Spirality",
            ["Determination of Size"] = "Determination of Size",
            ["Appearance"] = "AppearanceAfterWashing",
            ["CF to Washing"] = "CFtoWashing&Rubbing&Light",
            ["CF to Rubbing"] = "CFtoWashing&Rubbing&Light",
            ["CF to Light"] = "CFtoWashing&Rubbing&Light",
            ["CF to Perspiration"] = "CFtoPerspiration&Water",
            ["CF to Water"] = "CFtoPerspiration&Water",
            ["CF to Saliva"] = "CFtoSaliva&Sweat",
            ["CF to Sweat"] = "CFtoSaliva&Sweat",
            ["CF to Sea Water"] = "CFtoSeaWater&Cl",
            ["CF to Chlorinated Water"] = "CFtoSeaWater&Cl",
            ["Determination of the Fastening of Components"]= "Determination of FC",
        };
        private static readonly Dictionary<string, Dictionary<string[], string>> TemplateSheetNames = new()
        {
            ["DS to Washing"] = new Dictionary<string[], string>
            {
                {new[] { "Bra" }, "DStoWashing&DC-BSS" },
                {new[] { "Body/Allover suit" }, "DStoWashing&DC-BSS" },
                {new[] { "Slip" }, "DStoWashing&DC-BSS" },
                {new[] { "Shirt" },"DStoWashing&DC-STPSD"},
                {new[] { "Pullover" },"DStoWashing&DC-STPSD"},
                {new[] { "Top" },"DStoWashing&DC-STPSD"},
                {new[] { "Undershirt" },"DStoWashing&DC-STPSD"},
                {new[] { "Pants" },"DStoWashing&DC-STPSD"},
                {new[] { "Skirt" },"DStoWashing&DC-STPSD"},
                {new[] { "Dress" },"DStoWashing&DC-STPSD"},
                {new[] { "Baby-body suits" },"DStoWashing&DC-BBPSC"},
                {new[] { "Bib overall" },"DStoWashing&DC-BBPSC"},
                {new[] { "Panty pants" },"DStoWashing&DC-BBPSC"},
                {new[] { "Tights" },"DStoWashing&DC-BBPSC"},
                {new[] { "Socks" },"DStoWashing&DC-BBPSC"},
                {new[] { "Caps" },"DStoWashing&DC-BBPSC"},
                {new[] { "Fabric and Home Textile" },"DStoWashing-F"},
            },
        };
        private static readonly Dictionary<string, Func<string, string, string[]>> CellMapper = new()
        {
            ["Weight"] = (_, _) => ExcelKikMapper.MapWeight(),
            ["Piece Weight"] = (_, _) => ExcelKikMapper.MapWeight(),
            ["Yarn Count"] = (_, _) => ExcelKikMapper.MapYarnCount(),
            ["Pilling Resistance"] = (_, _) => ExcelKikMapper.MapPilling(),
            ["Zipper Strength"] = (_, _) => ExcelKikMapper.MapZipper(),
            ["Water Resistance-Hydrostatic Pressure"] = (_, _) => ExcelKikMapper.MapHydroatatic(),
            ["Air Permeability"] = (_, _) => ExcelKikMapper.MapAir(),
            ["Attachment Strength"] = (_, _) => ExcelKikMapper.MapAttachment(),
            ["Density"] = (_, _) => ExcelKikMapper.MapDensity(),

            ["Spirality/Skewing"] = (_, _) => ExcelKikMapper.MappSpirality(),
            ["Determination of Size"] = (_, _) => ExcelKikMapper.DeterminationOfSize(),
            ["Appearance"] = (_, _) => ExcelKikMapper.MapAppearance(),
            ["CF to Washing"] = (n, _) => ExcelKikMapper.MapWRL(n),
            ["CF to Rubbing"] = (n, _) => ExcelKikMapper.MapWRL(n),
            ["CF to Light"] = (n, _) => ExcelKikMapper.MapWRL(n),
            ["CF to Perspiration"] = (n, _) => ExcelKikMapper.MapPW(n),
            ["CF to Water"] = (n, _) => ExcelKikMapper.MapPW(n),
            ["CF to Saliva"] = (_, _) => ExcelKikMapper.MapCFtoSalivaSweat(),
            ["CF to Sweat"] = (_, _) => ExcelKikMapper.MapCFtoSalivaSweat(),
            ["CF to Sea Water"] = (n, _) => ExcelKikMapper.MapSC(n),
            ["CF to Chlorinated Water"] = (n, _) => ExcelKikMapper.MapSC(n),
            ["DS to Washing"] = (_, m) => ExcelKikMapper.MapDStoWashing(m),
            ["Determination of the Fastening of Components"] = (_, _) => ExcelKikMapper.MapDeterminationToFc(),
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
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>() ;
                if (dto.sampleDescription!.Contains("Fabric and Home Textile"))
                {
                    map["BC1"] = (w, dto, reportNo) => reportNo;
                    map["BA4"] = (w, dto, reportNo) => w.Temperature!;
                    map["BB5"] = (w, dto, reportNo) => w.WashingProcedure!;
                    map["AR6"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["BJ6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!;
                    map["AR7"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction) ? "-":w.SpecialCareInstruction;
                    map["AZ13"] = (w, dto, reportNo) => "1";
                    map["BQ6"] = (w, dto, reportNo) => w.Program!;
                }
                else
                {
                    map["P1"] = (w, dto, reportNo) => reportNo;
                    map["L4"] = (w, dto, reportNo) => w.Temperature!;
                    map["J5"] = (w, dto, reportNo) => w.WashingProcedure!;
                    map["AG5"] = (w, dto, reportNo) => w.DryProcedure!;
                    map["A6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!;
                    map["N6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction) ? "-" : w.SpecialCareInstruction;
                    map["W8"] = (w, dto, reportNo) => "1";
                    map["AG10"] = (w, dto, reportNo) => "1";
                    map["H6"] = (w, dto, reportNo) => w.Program!;
                }
                return map;
            },
            ["Appearance"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["BG6"] = (w, dto, reportNo) => "1",
                ["BE13"] = (w, dto, reportNo) => "1",
                ["BI13"] = (w, dto, reportNo) => w.IronMethod!,
                ["BA39"] = (w, dto, reportNo) => w.Temperature!,
                ["BE40"] = (w, dto, reportNo) => w.WashingProcedure!,
                ["AU41"] = (w, dto, reportNo) => w.DryProcedure!,
                ["BD41"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!,
                ["AR42"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!,
                ["BL41"] = (w, dto, reportNo) => w.Program!,
            },
            ["CF to Chlorinated Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A17"] = (w, dto, reportNo) => dto.Standard!
            },
            ["CF to Sea Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
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
            ["CF to Light"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A28"] = (w, dto, reportNo) => dto.Standard!,
                ["B31"] = (w, dto, reportNo) => dto.Parameter!,
            },
            ["CF to Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A27"] = (w, dto, reportNo) => dto.Standard!,
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
                ["G3"] = (w, dto, reportNo) => "√"
            },
            ["CF to Sweat"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["J3"] = (w, dto, reportNo) => "√"
            },
            ["Spirality/Skewing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AT6"] = (w, dto, reportNo) => "1",
                ["BR24"] = (w, dto, reportNo) => w.Temperature!,
                ["BH27"] = (w, dto, reportNo) => w.WashingProcedure!,
                ["BM28"] = (w, dto, reportNo) => w.DryProcedure!,
                ["BV28"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) == true ? "/ Iron" : w.IronMethod!,
                ["BH29"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!,
                ["BU26"] = (w, dto, reportNo) => w.Program!
            },
            ["Determination of Size"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
            },
            ["Determination of the Fastening of Components"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AR4"] = (w, dto, reportNo) => dto.Standard!,
                ["AT8"] = (w, dto, reportNo) =>"1",
                ["BR50"] = (w, dto, reportNo) => w.Temperature!,
                ["BJ51"] = (w, dto, reportNo) => w.DryProcedure!,
                ["AR52"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) == true ? "-" : w.SpecialCareInstruction!,
            },
        };

        private static readonly Dictionary<string, Func<WetParameterIso, CheckListDto, string, Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>>> PhyExtraMap = new()
        {
            ["Weight"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["J1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
            },
            ["Piece Weight"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["J1"] = (w, dto, reportNo) => reportNo,
                ["A3"] = (w, dto, reportNo) => dto.Standard!,
                ["P3"] = (w, dto, reportNo) => "条重",
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
            ["Water Resistance-Hydrostatic Pressure"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();

                map["M1"] = (wp, dto, reportNo) => reportNo;
                map["A3"] = (wp, dto, reportNo) => dto.Standard!;
                if (dto.Parameter!.Contains("1500"))
                {
                    map["E7"] = (wp, dto, reportNo) => "1500";
                    map["E15"] = (wp, dto, reportNo) => "1500";
                }
                else if (dto.Parameter!.Contains("3000"))
                {
                    map["E7"] = (wp, dto, reportNo) => "3000";
                    map["E15"] = (wp, dto, reportNo) => "3000";
                }
                else if (dto.Parameter!.Contains("800"))
                {
                    map["E7"] = (wp, dto, reportNo) => "800";
                    map["E15"] = (wp, dto, reportNo) => "800";
                }
                else if (dto.Parameter!.Contains("0"))
                {
                    map["E7"] = (wp, dto, reportNo) => "0";
                    map["E15"] = (wp, dto, reportNo) => "0";
                }
                else
                {
                    map["E7"] = (wp, dto, reportNo) => "N/A";
                    map["E15"] = (wp, dto, reportNo) => "N/A";
                }
                return map;
            },
            ["Air Permeability"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["M1"] = (wp, dto, reportNo) => reportNo,
                ["A3"] = (wp, dto, reportNo) => dto.Standard!,
                ["F5"] = (wp, dto, reportNo) => "100",
                ["E6"] = (wp, dto, reportNo) => "20",
            },
            ["Attachment Strength"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["M1"] = (wp, dto, reportNo) => reportNo;
                if (dto.Standard!.Contains("EN 71")|| dto.Standard!.Contains("16792"))
                {
                    map["A3"] = (wp, dto, reportNo) => dto.Standard!;
                    map["A18"] = (wp, dto, reportNo) => dto.Standard!;
                }
                else 
                {
                    map["A3"] = (wp, dto, reportNo) => "DIN EN 17394-2:2020"!;
                    map["A18"] = (wp, dto, reportNo) => "DIN CEN/TS 17394-3:2021";
                }
                return map;
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
            if(itemName=="DS to Washing"&&!sampleDescription.Contains("Fabric and Home Textile")) offset = 0;
            if (itemName == "Appearance")
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
