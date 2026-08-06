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

    public sealed class PrintNextExcel : IPrintExcelStrategy
    {
        private readonly LabDbContextSec _db;

        private Dictionary<string, int> _usedBaseSheets = new Dictionary<string, int>();

        public PrintNextExcel(LabDbContextSec db)
        {
            _db = db;
        }

        public void PrintJsonData(ExcelSubmitDto Dto, ExcelPackage PackageWet, ExcelPackage PackagePhy)
        {
            _usedBaseSheets.Clear();
            string reportNumber = Dto.ReportNumber!;
            string buyer = Dto.Buyer!;
            string menu = Dto.MenuName!;
            string sampleDescription = Dto.SampleDescription!;
            var selectedRows = Dto.SelectedRows;

            List<CheckListDto> checkLists = new List<CheckListDto>();
            foreach (var row in selectedRows!) checkLists.Add(new CheckListDto().CreateDto(row, menu, sampleDescription));
            #region 附加项配置
            // 1. 检查并添加 Mass Per Unit Area
            var massPerUnitAreaRow = checkLists.FirstOrDefault(row =>
                new[] { "Tear Strength", "Grab Strength & Seam Slippage", "Seam Slippage of Garment Seams","Bursting Strength", "Martindale Abrasion" }
                    .Contains(row.ItemName));

            if (massPerUnitAreaRow != null && checkLists.FirstOrDefault(row =>row.ItemName=="Mass per Unit Area" )==null)
            {
                checkLists.Add(new CheckListDto
                {
                    ItemName = "Mass per Unit Area",
                    Standard = "TM20",
                    Parameter = "Single unit weight",
                    Type = "Physics",
                    Sample = massPerUnitAreaRow.Sample,
                    Extra = null,
                    MenuName = menu,
                    sampleDescription = sampleDescription,
                });
            }

            // 2. 检查并添加 Appearance Assessment after Wash
            var washAssessmentRow = checkLists.FirstOrDefault(row =>
                new[] { "Stability to Washing" }
                    .Contains(row.ItemName) &&
                sampleDescription.Contains("Garment"));

            if (washAssessmentRow != null)
            {
                checkLists.Add(new CheckListDto
                {
                    ItemName = "Appearance Assessment after Wash",
                    Standard = "TM9",
                    Parameter = "Same as Stability to Washing",
                    Type = "Wet",
                    Sample = washAssessmentRow.Sample,
                    Extra = null,
                    MenuName = menu,
                    sampleDescription = sampleDescription,
                });
            }

            // 3. 检查并添加 Appearance Assessment after Dry Cleaning
            var dryCleanAssessmentRow = checkLists.FirstOrDefault(row =>
                new[] { "Stability to Dry Cleaning" }
                    .Contains(row.ItemName) &&
                sampleDescription.Contains("Garment"));

            if (dryCleanAssessmentRow != null)
            {
                checkLists.Add(new CheckListDto
                {
                    ItemName = "Appearance Assessment after Dry Cleaning",
                    Standard = "TM9a",
                    Parameter = "Same as Stability to Dry Cleaning",
                    Type = "Wet",
                    Sample = dryCleanAssessmentRow.Sample,
                    Extra = null,
                    MenuName = menu,
                    sampleDescription = sampleDescription,
                });
            }

            // 4. 检查并添加 Spirality
            var spiralityRow = checkLists.FirstOrDefault(row =>
                new[] { "Stability to Washing", "Stability to Dry Cleaning" }
                    .Contains(row.ItemName) &&
                sampleDescription.Contains("Weft"));

            if (spiralityRow != null)
            {
                checkLists.Add(new CheckListDto
                {
                    ItemName = "Spirality",
                    Standard = "TM13",
                    Parameter = "Same as Stability",
                    Type = "Wet",
                    Sample = spiralityRow.Sample,
                    Extra = null,
                    MenuName = menu,
                    sampleDescription = sampleDescription,
                });
            }
            #endregion
            foreach (var dto in checkLists)
            {
                Console.WriteLine($"{dto.ItemName} -> {dto.Type}");
                var pkg = dto.Type == "Wet" ? PackageWet : PackagePhy;
                if (TemplateSheetNames.ContainsKey(dto.ItemName!) || TemplateSheetNamesNormal.ContainsKey(dto.ItemName!))
                    FillSheet(pkg, dto.ItemName!, dto, Dto, reportNumber);
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
            var tplName = new TemplateSelector(TemplateSheetNames, TemplateSheetNamesNormal).GetTemplateName(itemName,dto.sampleDescription!);

            // 修正：只有同一个 ItemName 重复使用同一个模板时，才判定为需要防覆盖
            string baseSheetKey = $"{itemName}_{pkg.GetHashCode()}_{tplName}";
            bool needsCopyBase = false;
            int copyIndex = 0;

            if (_usedBaseSheets.ContainsKey(baseSheetKey))
            {
                // 只有同一个ItemName（如附加外观和原始外观）第二次使用该模板时才创建副本
                needsCopyBase = true;
                copyIndex = ++_usedBaseSheets[baseSheetKey];
            }
            else
            {
                _usedBaseSheets[baseSheetKey] = 0;
            }

            // 获取原始模板
            var originalTemplate = pkg.Workbook.Worksheets[tplName];
            ExcelWorksheet template;

            // 如果需要防覆盖，则复制一个独立的基础Sheet
            if (needsCopyBase)
            {
                string copySheetName = $"{tplName}_Copy{copyIndex}";
                if (pkg.Workbook.Worksheets.Any(ws => ws.Name == copySheetName))
                {
                    template = pkg.Workbook.Worksheets[copySheetName];
                }
                else
                {
                    template = pkg.Workbook.Worksheets.Copy(tplName, copySheetName);
                }
            }
            else
            {
                template = originalTemplate;
            }

            // 2) 计算需要几张 sheet
            var cellAddrs = CellMapper[itemName](itemName, dto.sampleDescription!);
            string[]? AfterWashCellAddrs = null;
            if (itemName == "Stability to Washing" || itemName == "Stability to Dry Cleaning")
            {
                AfterWashCellAddrs = AfterWashCellMapper[itemName](itemName, dto.sampleDescription!);
            }


            //<--------------------需要引入afterWash变量，缩水参数中的Iron变量----------------------->
            var samples = dto.Sample!.Split(',').Select(s => s.Trim()).ToArray();
            if (itemName == "Air Permeability of Textile Fabrics")
            {
                samples = dto.Sample!
                    .Split(',')
                    .Select(s => s.Trim())
                    .SelectMany(s => new[] { s, $"{s} - 1 Wash"})
                    .ToArray();
            }

            int[]? afterWashMap = null;
            if (itemName == "Stability to Washing" || itemName == "Stability to Dry Cleaning")
            {
                var wp = _db.WetParameterIsos
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                if (wp == null) wp = new WetParameterIso();
                var afterWash = string.Empty;
                if (itemName == "Stability to Washing" || itemName == "Stability to Dry Cleaning")
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
            if (itemName == "WIRA Steam Stability"||itemName== "Hydrostatic Head Test") { capacity = 3; }// 特例处理，实际容量为3
            if (itemName == "Fastness to Perspiration") { capacity = 6; }// 特例处理，实际容量为6
            if(itemName== "Accelerotor Pile Loss"||itemName.Contains("Extension")) { capacity = 2; }// 特例处理，实际容量为2
            if (itemName.Contains("Appearance")||itemName=="Abrasion Home"||itemName== "Moisture Management") { capacity = 1; }
            if (itemName == "Stability to Washing"|| itemName == "Stability to Dry Cleaning" && dto.sampleDescription!.Contains("Garment")) { capacity = 1; }
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
                    // 后续分页基于基础 template 进行复制
                    string newSheetName = $"{template.Name} ({idx + 1})";
                    // 检查是否已经存在同名的 sheet
                    if (pkg.Workbook.Worksheets.Any(ws => ws.Name == newSheetName))
                    {
                        ws = pkg.Workbook.Worksheets[newSheetName];
                    }
                    else
                    {
                        ws = pkg.Workbook.Worksheets.Copy(template.Name, newSheetName);
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
                    // 新增：判断是否为附加的外观项（通过 Parameter 内容识别）
                    string queryItemName = itemName;
                    if (itemName == "Appearance Assessment after Wash" &&
                        dto.Parameter != null &&
                        dto.Parameter.Contains("Same as Stability"))
                    {
                        queryItemName = "Stability to Washing"; // 附加外观使用 Stability to Washing 的数据
                    }

                    var wp = _db.WetParameterIsos.FirstOrDefault(p => p.ContactItem == queryItemName && p.ReportNumber == reportNo);
                    if (wp == null)
                    {
                        wp = _db.WetParameterIsos
                            .FirstOrDefault(p => p.ReportNumber == reportNo &&
                                (p.ContactItem == "Stability to Washing" || p.ContactItem == "Stability to Dry Cleaning"));
                    }
                    // 提前获取默认值或实际值
                    var wetParameter = wp ?? new WetParameterIso();
                    var extraMap = WetExtraMap.GetValueOrDefault(itemName, (wp, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>())(wp!, dto, reportNo);
                    // 使用foreach循环填充值
                    foreach (var kv in extraMap)
                    {
                        ws.Cells[kv.Key].Value = kv.Value(wetParameter, dto, reportNo);
                    }
                }
                else if (dto.Type == "Physics")
                {
                    var wp = new WetParameterIso();
                    if (itemName == "Pilling Resistance"
                        ||itemName== "Spray Rating"
                        || itemName == "Swiss Pilling"
                        || itemName == "Accelerotor Pile Loss"
                        ||itemName == "Moisture Management" 
                        || itemName == "Hydrostatic Head Test"
                        ||itemName == "Air Permeability of Textile Fabrics")
                    {
                        wp = _db.WetParameterIsos
                            .FirstOrDefault(p => p.ReportNumber == reportNo &&
                                (p.ContactItem == "Stability to Washing" || p.ContactItem == "Stability to Dry Cleaning"));
                    }
                    else
                    {
                         wp = _db.WetParameterIsos
                                .FirstOrDefault(p => p.ContactItem == itemName && p.ReportNumber == reportNo);
                    }  
                    // 提前获取默认值或实际值
                    var wetParameter = wp ?? new WetParameterIso();
                    var extraMap = PhyExtraMap.GetValueOrDefault(itemName, (wp, dto, esDto,ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>())(wp!, dto, esDto, ws, reportNo);
                    // 使用foreach循环填充值
                    foreach (var kv in extraMap)
                    {
                        ws.Cells[kv.Key].Value = kv.Value(wetParameter, dto, esDto, ws, reportNo);
                    }
                }
            }


        }
        private static readonly Dictionary<string, string> TemplateSheetNamesNormal = new()
        {
            ["Fastness to Light"] = "TM1-TM2-TM3",
            ["Fastness to Washing"] = "TM1-TM2-TM3",
            ["Cross Staining to Washing"] = "TM1-TM2-TM3",
            ["Fastness to Dry Cleaning"] = "TM1-TM2-TM3",
            ["Cross Staining to Dry Cleaning"] = "TM1-TM2-TM3",
            ["Fastness to Water"] = "TM4-TM5-TM6-TM43",
            ["Cross Staining to Water"] = "TM4-TM5-TM6-TM43",
            ["Fastness to Chlorinated Water"] = "TM4-TM5-TM6-TM43",
            ["Fastness to Rubbing"] = "TM4-TM5-TM6-TM43",
            ["Phenolic Yellowing"] = "TM4-TM5-TM6-TM43",
            ["Fastness to Perspiration"] = "TM52",
            ["WIRA Steam Stability"] = "TM15-TM24",
            ["Assessment of Easy to Iron Fabrics"] = "TM15-TM24",
            ["Print Durability"] = "TM7",
            ["Embellishment Durability (Childrenswear)"] = "TM7a",
            ["Embellishment Durability (General)"] = "TM7b",
            ["Foil Durability"] = "TM7c",
            ["Appearance Assessment after Wash"] = "TM9-TM9a",
            ["Appearance Assessment after Dry Clean"] = "TM9-TM9a",
            ["Polar Fleece Assessment"] = "TM11",
            ["Fastness to Saliva"]="TM48",
            ["Fastness to Sea Water"] = "TM51-TM55",
            ["Fastness to Oxidative Bleach"] = "TM51-TM55",

            ["Grab Strength & Seam Slippage"] = "TM16",
            ["Seam Slippage of Garment Seams"] = "TM16a",
            ["Tear Strength"] = "TM17",
            ["Mass per Unit Area"] = "TM25-TM20",
            ["Wing Rip Tear Strength"] = "TM25-TM20",
            ["Martindale Abrasion"] = "TM18-TM18a",
            ["Abrasion Home"] = "TM18-TM18a",
            ["Pilling Resistance"] = "TM19",
            ["Extension and Modulus"] = "TM21",
            ["Extension and Recovery"] = "TM21a",
            ["Bursting Strength"] = "TM22-TM23",
            ["Spray Rating"] = "TM22-TM23",
            ["Swiss Pilling"] = "TM26",
            ["Accelerotor Pile Loss"] = "TM31",
            ["Attachment Strength"] = "TM42",
            ["Moisture Management"] = "TM58",
            ["Snagging Resistance"] = "TM59",
            ["Mass per Unit Length"] = "TM62",
            ["Fabric Width"] = "TM63",
            ["Course and Wales"] = "TM64-TM65",
            ["Ends & Picks"] = "TM64-TM65",
            ["Hydrostatic Head Test"] ="Water Resistance",
            ["Air Permeability of Textile Fabrics"] = "Air Permeability",
            ["Yarn Count"] = "Yarn Count"
        };
        private static readonly Dictionary<string, Dictionary<string[], string>> TemplateSheetNames = new()
        {
            ["Stability to Washing"] = new Dictionary<string[], string>
            {
                {new[] { "Fabric" }, "TM12-TM14-TM13-F" },
                {new[] { "Garment" },"TM12-TM14-TM13-G"},
                {new[] { "Socks" }, "TM12-TM14-TM13-Acc" },
                {new[] { "Gloves" }, "TM12-TM14-TM13-Acc" },
                {new[] { "Cap" }, "TM12-TM14-TM13-Acc" },
            },
            ["Spirality"] = new Dictionary<string[], string>
            {
                { new[] {"Fabric" }, "TM12-TM14-TM13-F" },
                {new[] { "Garment" },"TM12-TM14-TM13-G"},
                {new[] { "Socks" }, "TM12-TM14-TM13-Acc" },
                {new[] { "Gloves" }, "TM12-TM14-TM13-Acc" },
                {new[] { "Cap" }, "TM12-TM14-TM13-Acc" },
            },
            ["Stability to Dry Cleaning"] = new Dictionary<string[], string>
            {
                {new[] { "Fabric" }, "TM12-TM14-TM13-F" },
                {new[] { "Garment" },"TM12-TM14-TM13-G"},
                {new[] { "Socks" }, "TM12-TM14-TM13-Acc" },
                {new[] { "Gloves" }, "TM12-TM14-TM13-Acc" },
                {new[] { "Cap" }, "TM12-TM14-TM13-Acc" },
            },
        };
        private static readonly Dictionary<string, Func<string, string, string[]>> CellMapper = new()
        {
            ["Fastness to Light"] = (n, m) => ExcelNextMapper.MapTM1TM2TM3(n),
            ["Fastness to Washing"] = (n, m) => ExcelNextMapper.MapTM1TM2TM3(n),
            ["Cross Staining to Washing"] = (n, m) => ExcelNextMapper.MapTM1TM2TM3(n),
            ["Fastness to Dry Cleaning"] = (n, m) => ExcelNextMapper.MapTM1TM2TM3(n),
            ["Cross Staining to Dry Cleaning"] = (n, m) => ExcelNextMapper.MapTM1TM2TM3(n),
            ["Fastness to Water"] = (n, m) => ExcelNextMapper.MapTM4TM5TM36TM43(n),
            ["Cross Staining to Water"] = (n, m) => ExcelNextMapper.MapTM4TM5TM36TM43(n),
            ["Fastness to Chlorinated Water"] = (n, m) => ExcelNextMapper.MapTM4TM5TM36TM43(n),
            ["Fastness to Rubbing"] = (n, m) => ExcelNextMapper.MapTM4TM5TM36TM43(n),
            ["Phenolic Yellowing"] = (n, m) => ExcelNextMapper.MapTM4TM5TM36TM43(n),
            ["Fastness to Perspiration"] = (n, m) => ExcelNextMapper.MapTM52(),
            ["WIRA Steam Stability"] = (n, m) => ExcelNextMapper.MapTM15TM24(n),
            ["Assessment of Easy to Iron Fabrics"] = (n, m) => ExcelNextMapper.MapTM15TM24(n),
            ["Print Durability"] = (n, m) => ExcelNextMapper.MapTM7TM7aTM7bTM7c(),
            ["Embellishment Durability (Childrenswear)"] = (n, m) => ExcelNextMapper.MapTM7TM7aTM7bTM7c(),
            ["Embellishment Durability (General)"] = (n, m) => ExcelNextMapper.MapTM7TM7aTM7bTM7c(),
            ["Foil Durability"] = (n, m) => ExcelNextMapper.MapTM7TM7aTM7bTM7c(),
            ["Appearance Assessment after Wash"] = (n, m) => ExcelNextMapper.MapTM9TM9a(),
            ["Appearance Assessment after Dry Clean"] = (n, m) => ExcelNextMapper.MapTM9TM9a(),
            ["Polar Fleece Assessment"] = (n, m) => ExcelNextMapper.MapTM11(),
            ["Fastness to Saliva"] = (n, m) => ExcelNextMapper.MapTM48(),
            ["Fastness to Sea Water"] = (n, m) => ExcelNextMapper.MapTM51(),
            ["Fastness to Oxidative Bleach"] = (n, m) => ExcelNextMapper.MapTM55(),
            ["Stability to Washing"] = (n, m) => ExcelNextMapper.MapTM12TM14(m),
            ["Spirality"] = (n, m) => ExcelNextMapper.MapTM13(m),
            ["Stability to Dry Cleaning"] = (n, m) => ExcelNextMapper.MapTM12TM14(m),
            ["Grab Strength & Seam Slippage"] = (n, m) => ExcelNextMapper.MapTM16(),
            ["Seam Slippage of Garment Seams"] = (n, m) => ExcelNextMapper.MapTM16a(),
            ["Tear Strength"] = (n, m) => ExcelNextMapper.MapTM17(),
            ["Mass per Unit Area"] = (n, m) => ExcelNextMapper.MapTM25TM20(n),
            ["Wing Rip Tear Strength"] = (n, m) => ExcelNextMapper.MapTM25TM20(n),
            ["Martindale Abrasion"] = (n, m) => ExcelNextMapper.MapTM18TM18a(n),
            ["Abrasion Home"] = (n, m) => ExcelNextMapper.MapTM18TM18a(n),
            ["Pilling Resistance"] = (n, m) => ExcelNextMapper.MapTM19(),
            ["Extension and Modulus"] = (n, m) => ExcelNextMapper.MapTM21(),
            ["Extension and Recovery"] = (n, m) => ExcelNextMapper.MapTM21a(),
            ["Bursting Strength"] = (n, m) => ExcelNextMapper.MapTM22TM23(n),
            ["Spray Rating"] = (n, m) => ExcelNextMapper.MapTM22TM23(n),
            ["Swiss Pilling"] = (n, m) => ExcelNextMapper.MapTM26(),
            ["Accelerotor Pile Loss"] = (n, m) => ExcelNextMapper.MapTM31(),
            ["Attachment Strength"] = (n, m) => ExcelNextMapper.MapTM42(),//无工作单
            ["Moisture Management"] = (n, m) => ExcelNextMapper.MapTM58(),
            ["Snagging Resistance"] = (n, m) => ExcelNextMapper.MapTM59(),
            ["Mass per Unit Length"] = (n, m) => ExcelNextMapper.MapTM62(),
            ["Fabric Width"] = (n, m) => ExcelNextMapper.MapTM63(),
            ["Course and Wales"] = (n, m) => ExcelNextMapper.MapTM64TM65(),
            ["Ends & Picks"] = (n, m) => ExcelNextMapper.MapTM64TM65(),
            ["Hydrostatic Head Test"] = (n, m) => ExcelNextMapper.MapHydrostatic(),
            ["Air Permeability of Textile Fabrics"] = (n, m) => ExcelNextMapper.MapAir(),
            ["Yarn Count"] = (n, m) => ExcelNextMapper.MapYarnCount(),

        };
        //取洗涤遍数映射地址的函数
        private static readonly Dictionary<string, Func<string, string, string[]>> AfterWashCellMapper = new()
        {
            ["Stability to Washing"] = (n, m) => ExcelNextMapper.WashingAf(m),
            ["Stability to Dry Cleaning"] = (n, m) => ExcelNextMapper.WashingAf(m),
        };
        private static readonly Dictionary<string, Func<WetParameterIso, CheckListDto, string, Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>>> WetExtraMap = new()
        {
            ["Fastness to Light"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
            },
            ["Fastness to Washing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["B10"] = (w, dto, reportNo) => w.Program!,
                ["E10"] = (w, dto, reportNo) => w.Temperature!,
                ["E11"] = (w, dto, reportNo) => w.SteelBallNum.ToString()!,
            },
            ["Cross Staining to Washing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["B10"] = (w, dto, reportNo) => w.Program!,
                ["E10"] = (w, dto, reportNo) => w.Temperature!,
                ["E11"] = (w, dto, reportNo) => w.SteelBallNum.ToString()!,
            },
            ["Fastness to Dry Cleaning"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
            },
            ["Cross Staining to Dry Cleaning"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
            },
            ["Fastness to Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
            },
            ["Cross Staining to Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
            },
            ["Fastness to Chlorinated Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
            },
            ["Fastness to Rubbing"] = (w, dto, reportNo) => 
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["D1"] = (w, dto, reportNo) => reportNo;
                map["L23"] = (w, dto, reportNo) => dto.sampleDescription!.Contains("dry rubbing only") ? "dry rubbing only"
                : dto.sampleDescription.Contains("wet rubbing only") ?"wet rubbing only"
                :"-";
                return map;
            },
            ["Phenolic Yellowing"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
            },
            ["Fastness to Perspiration"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
            },
            ["Stability to Washing"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["BC1"] = (w, dto, reportNo) => reportNo;
                    map["BS2"] = (w, dto, reportNo) => "TM12";
                    map["AX3"] = (w, dto, reportNo) => w!.WashingProcedure!;
                    map["BY3"] = (w, dto, reportNo) => w!.Temperature!;
                    map["BG4"] = (w, dto, reportNo) => w!.Ballast!;
                    map["BF5"] = (w, dto, reportNo) => w!.DryProcedure!;
                    map["BO5"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) ? "/" : w.IronMethod!;             
                    map["AR6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) ? "-" : w.SpecialCareInstruction!;
                }
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["P1"] = (w, dto, reportNo) => reportNo;
                    map["AF2"] = (w, dto, reportNo) => "TM12";
                    map["I3"] = (w, dto, reportNo) => w!.WashingProcedure!;
                    map["AK3"] = (w, dto, reportNo) => w!.Temperature!;
                    map["T4"] = (w, dto, reportNo) => w!.Ballast!;
                    map["S5"] = (w, dto, reportNo) => w!.DryProcedure!;
                    map["AB5"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) ? "/" : w.IronMethod!;
                    map["A6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) ? "-" : w.SpecialCareInstruction!;
                }
                if (dto.sampleDescription!.Contains("Cap") || dto.sampleDescription!.Contains("Socks") || dto.sampleDescription!.Contains("Gloves"))
                {
                    map["N1"] = (w, dto, reportNo) => reportNo;
                    map["AE2"] = (w, dto, reportNo) => "TM12";
                    map["G3"] = (w, dto, reportNo) => w!.WashingProcedure!;
                    map["AK3"] = (w, dto, reportNo) => w!.Temperature!;
                    map["S4"] = (w, dto, reportNo) => w!.Ballast!;
                    map["Q5"] = (w, dto, reportNo) => w!.DryProcedure!;
                    map["Z5"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) ? "/" : w.IronMethod!;
                    map["A6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) ? "-" : w.SpecialCareInstruction!;
                }
                return map;
            },
            ["Spirality"] = (w, dto, reportNo) => 
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["BC1"] = (w, dto, reportNo) => reportNo;
                    map["BM40"] = (w, dto, reportNo) => "1";
                    map["AX3"] = (w, dto, reportNo) => w!.WashingProcedure!;
                    map["BY3"] = (w, dto, reportNo) => w!.Temperature!;
                    map["BG4"] = (w, dto, reportNo) => w!.Ballast!;
                    map["BF5"] = (w, dto, reportNo) => w!.DryProcedure!;
                    map["BO5"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) ? "/" : w.IronMethod!;
                    map["AR6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) ? "-" : w.SpecialCareInstruction!;
                }
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["P1"] = (w, dto, reportNo) => reportNo;
                    map["Z42"] = (w, dto, reportNo) => "1";
                    map["I3"] = (w, dto, reportNo) => w!.WashingProcedure!;
                    map["AK3"] = (w, dto, reportNo) => w!.Temperature!;
                    map["T4"] = (w, dto, reportNo) => w!.Ballast!;
                    map["S5"] = (w, dto, reportNo) => w!.DryProcedure!;
                    map["AB5"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) ? "/" : w.IronMethod!;
                    map["A6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) ? "-" : w.SpecialCareInstruction!;
                }
                if (dto.sampleDescription!.Contains("Cap") || dto.sampleDescription!.Contains("Socks") || dto.sampleDescription!.Contains("Gloves"))
                {
                    map["N1"] = (w, dto, reportNo) => reportNo;
                    map["Z47"] = (w, dto, reportNo) => "1";
                    map["G3"] = (w, dto, reportNo) => w!.WashingProcedure!;
                    map["AK3"] = (w, dto, reportNo) => w!.Temperature!;
                    map["S4"] = (w, dto, reportNo) => w!.Ballast!;
                    map["Q5"] = (w, dto, reportNo) => w!.DryProcedure!;
                    map["Z5"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.Iron!) ? "/" : w.IronMethod!;
                    map["A6"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) ? "-" : w.SpecialCareInstruction!;
                }
                return map;
            },
            ["Stability to Dry Cleaning"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                if (dto.sampleDescription!.Contains("Fabric"))
                {
                    map["BC1"] = (w, dto, reportNo) => reportNo;
                    map["BS2"] = (w, dto, reportNo) => "TM14";
                    map["BC7"] = (w, dto, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal";
                }
                else if (dto.sampleDescription!.Contains("Garment"))
                {
                    map["P1"] = (w, dto, reportNo) => reportNo;
                    map["AF2"] = (w, dto, reportNo) => "TM14";
                    map["N7"] = (w, dto, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal";
                }
                if (dto.sampleDescription!.Contains("Cap") || dto.sampleDescription!.Contains("Socks") || dto.sampleDescription!.Contains("Gloves"))
                {
                    map["N1"] = (w, dto, reportNo) => reportNo;
                    map["AE2"] = (w, dto, reportNo) => "TM14";
                    map["L7"] = (w, dto, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal";
                }
                return map;
            },
            ["WIRA Steam Stability"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
            },
            ["Assessment of Easy to Iron Fabrics"] = (w, dto, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>();
                map["BC1"] = (w, dto, reportNo) => reportNo;
                map["AT30"] = (w, dto, reportNo) => "1";
                if (string.IsNullOrEmpty(w.Sensitive) == false)
                {
                    map["BC42"] = (w, dto, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal";
                }
                else
                {
                    map["AX38"] = (w, dto, reportNo) => w!.WashingProcedure!;
                    map["BY38"] = (w, dto, reportNo) => w!.Temperature!;
                    map["BG39"] = (w, dto, reportNo) => w!.Ballast!;
                    map["BJ40"] = (w, dto, reportNo) => w!.IronMethod!;
                }
                return map;
            },
            ["Print Durability"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["BG27"] = (w, dto, reportNo) => w.Ballast!,
                ["BF3"] = (w, dto, reportNo) => "1"
            },
            ["Embellishment Durability (Childrenswear)"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["BG27"] = (w, dto, reportNo) => w.Ballast!,
                ["BF3"] = (w, dto, reportNo) => "1"
            },
            ["Embellishment Durability (General)"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AX28"] = (w, dto, reportNo) => w!.WashingProcedure!,
                ["BY28"] = (w, dto, reportNo) => w!.Temperature!,
                ["BG29"] = (w, dto, reportNo) => w.Ballast!,
                ["BI30"] = (w, dto, reportNo) => w!.DryProcedure!,
                ["BP30"] = (w, dto, reportNo) => "/",
            },
            ["Foil Durability"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["AX25"] = (w, dto, reportNo) => w!.WashingProcedure!,
                ["BY25"] = (w, dto, reportNo) => w!.Temperature!,
                ["BG26"] = (w, dto, reportNo) => w.Ballast!,
                ["BE27"] = (w, dto, reportNo) => w!.DryProcedure!,
                ["BL27"] = (w, dto, reportNo) =>  "/",
            },
            ["Appearance Assessment after Wash"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["BP2"] = (w, dto, reportNo) => "TM9",
                ["BK5"] = (w, dto, reportNo) => "1",
                ["BE12"] = (w, dto, reportNo) => "1",
                ["BI12"] = (w, dto, reportNo) => w.IronMethod!,
                ["AX42"] = (w, dto, reportNo) => w!.WashingProcedure!,
                ["BY42"] = (w, dto, reportNo) => w!.Temperature!,
                ["BG43"] = (w, dto, reportNo) => w.Ballast!,
                ["AR44"] = (w, dto, reportNo) => w!.Detergent!,
                ["BH44"] = (w, dto, reportNo) => w!.DryProcedure!,
                ["BO44"] = (w, dto, reportNo) => "/",
                ["AR46"] = (w, dto, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) ? "-" : w.SpecialCareInstruction!,
            },
            ["Appearance Assessment after Dry Clean"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["BP2"] = (w, dto, reportNo) => "TM9a",
                ["BC45"] = (w, dto, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal"
            },
            ["Polar Fleece Assessment"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["BC1"] = (w, dto, reportNo) => reportNo,
                ["BG27"] = (w, dto, reportNo) => w.Ballast!,
            },
            ["Fastness to Saliva"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["G3"] = (w, dto, reportNo) => "√",
            },
            ["Fastness to Sea Water"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
                ["G3"] = (w, dto, reportNo) => "√",
            },
            ["Fastness to Oxidative Bleach"] = (w, dto, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, string, string>>
            {
                ["D1"] = (w, dto, reportNo) => reportNo,
            },
        };
        private static readonly Dictionary<string, Func<WetParameterIso,CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>>> PhyExtraMap = new()
        {
            ["Grab Strength & Seam Slippage"] = (w, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (w, dto, esDto, ws, reportNo) => reportNo,
            },
            ["Seam Slippage of Garment Seams"] = (w, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (wp, dto, esDto, ws, reportNo) => esDto.ReportNumber!;
                var sample = ws.Cells["A7"].Value?.ToString();

                var cellOrder = new List<string> { "A9", "A11", "A13", "A15", "A17", "A19", "A21", "A23", "A25" };
                var reasonCellOrder = cellOrder.Select(c => "P" + c.Substring(1)).ToList();
                if (sample.ToLower().Contains("shell"))
                {
                    reasonCellOrder = cellOrder.Select(c => "H" + c.Substring(1)).ToList();
                }
                if (sample.ToLower().Contains("lining"))
                {
                    reasonCellOrder = cellOrder.Select(c => "P" + c.Substring(1)).ToList();
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
                return map;
            },
            ["Tear Strength"] = (w, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (w, dto, esDto, ws, reportNo) => reportNo,
            },
            ["Mass per Unit Area"] = (w, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (w, dto, esDto, ws, reportNo) => reportNo,
                ["AD26"] = (w, dto, esDto, ws, reportNo) => dto.Parameter!.Contains("Single")?"刻一个":"-",
            },
            ["Wing Rip Tear Strength"] = (w, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (w, dto, esDto, ws, reportNo) => reportNo,
            },
            ["Martindale Abrasion"] = (w, dto, esDto, ws, reportNo) => 
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (w, dto, esDto, ws, reportNo) => reportNo;
                if(dto.sampleDescription!.Contains("Stretch")) map["A3"] = (w, dto, esDto, ws, reportNo) => "{10000 rubs}";
                else map["A3"] = (w, dto, esDto, ws, reportNo) => "{≤150g/m²: 10000 rubs；150-250g/m²: 15000 rubs；≥250g/m²: 20000 rubs}";  
                return map;
            },
            ["Abrasion Home"] = (w, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (w, dto, esDto, ws, reportNo) => reportNo,
            },
            ["Pilling Resistance"] = (w, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (w, dto, esDto, ws, reportNo) => reportNo;
                map["D6"] = (w, dto, esDto, ws, reportNo) => dto.Parameter!.Contains("18000")?"18000 revs":"7200 revs";
                if (string.IsNullOrEmpty(w.DryCleanProcedure) == false)
                {
                    map["U4"] = (w, dto, esDto, ws, reportNo) => "√";
                    map["L43"] = (w, dto, esDto, ws, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal";
                }
                else
                {
                    map["J4"] = (w, dto, esDto, ws, reportNo) => "√";
                    map["G38"] = (w, dto, esDto, ws, reportNo) => w!.WashingProcedure!;
                    map["AK38"] = (w, dto, esDto, ws, reportNo) => w!.Temperature!;
                    map["Q39"] = (w, dto, esDto, ws, reportNo) => w!.Ballast!;
                    map["P40"] = (w, dto, esDto, ws, reportNo) => w!.DryProcedure!;
                    map["Y40"] = (w, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(w.Iron!) ? "/ Iron" : w.IronMethod!;
                    map["A41"] = (w, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) ? "-" : w.SpecialCareInstruction!;
                }
                return map;
            },
            ["Extension and Modulus"] = (w, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (w, dto, esDto, ws, reportNo) => reportNo;
                map["C4"] = (w, dto, esDto, ws, reportNo) => 
                dto.Parameter!.Contains("4.0") ? "4.0"
                : dto.Parameter!.Contains("1.5") ? "1.5" 
                : dto.Parameter!.Contains("2.5") ? "2.5":"3.6";
                map["E22"] = (w, dto, esDto, ws, reportNo) => dto.Parameter!.Contains("10") ? "10" : "40";
                return map;
            },
            ["Extension and Recovery"] = (w, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (w, dto, esDto, ws, reportNo) => reportNo;
                map["C4"] = (w, dto, esDto, ws, reportNo) => dto.Parameter!.Contains("4") ? "4.0" : "2.0";
                return map;
            },
            ["Bursting Strength"] = (w, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (w, dto, esDto, ws, reportNo) => reportNo,
            },
            ["Spray Rating"] = (w, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (w, dto, esDto, ws, reportNo) => reportNo;
                map["V32"] = (w, dto, esDto, ws, reportNo) => dto.sampleDescription!.Contains("Teflon Coated")?"10"
                : dto.sampleDescription.Contains("Coated") ? "5"
                :"1";
                if (string.IsNullOrWhiteSpace(w.Sensitive)==false)
                {
                    map["L46"] = (w, dto, esDto, ws, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal";
                }
                else
                {
                    map["G41"] = (w, dto, esDto, ws, reportNo) => w!.WashingProcedure!;
                    map["AJ41"] = (w, dto, esDto, ws, reportNo) => w!.Temperature!;
                    map["Q42"] = (w, dto, esDto, ws, reportNo) => w!.Ballast!;
                    map["P43"] = (w, dto, esDto, ws, reportNo) => w!.DryProcedure!;
                    map["Y43"] = (w, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(w.Iron!) ? "/" : w.IronMethod!;
                    map["A44"] = (w, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) ? "-" : w.SpecialCareInstruction!;
                }
                return map;
            },
            ["Swiss Pilling"] = (w, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (w, dto, esDto, ws, reportNo) => reportNo;
                map["H8"] = (w, dto, esDto, ws, reportNo) => dto.Parameter!.Contains("500") ? "500revs" : "1000revs";
                map["O8"] = (w, dto, esDto, ws, reportNo) => dto.Parameter!.Contains("500") ? "1000revs" : "2000revs";
                map["V8"] = (w, dto, esDto, ws, reportNo) => dto.Parameter!.Contains("500") ? "2000revs" : "4000revs";
                if (string.IsNullOrEmpty(w.Sensitive) == false)
                {
                    map["U4"] = (w, dto, esDto, ws, reportNo) => "√";
                    map["L42"] = (w, dto, esDto, ws, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal";
                }
                else
                {
                    map["J4"] = (w, dto, esDto, ws, reportNo) => "√";
                    map["G37"] = (w, dto, esDto, ws, reportNo) => w!.WashingProcedure!;
                    map["AK37"] = (w, dto, esDto, ws, reportNo) => w!.Temperature!;
                    map["Q38"] = (w, dto, esDto, ws, reportNo) => w!.Ballast!;
                    map["P39"] = (w, dto, esDto, ws, reportNo) => w!.DryProcedure!;
                    map["Y39"] = (w, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(w.Iron!) ? "/" : w.IronMethod!;
                    map["A40"] = (w, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) ? "-" : w.SpecialCareInstruction!;
                }
                return map;
            },
            ["Accelerotor Pile Loss"] = (w, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (w, dto, esDto, ws, reportNo) => reportNo;
                if (string.IsNullOrEmpty(w.Sensitive) == false)
                {
                    map["L50"] = (w, dto, esDto, ws, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal";
                }
                else
                {
                    map["G45"] = (w, dto, esDto, ws, reportNo) => w!.WashingProcedure!;
                    map["AK45"] = (w, dto, esDto, ws, reportNo) => w!.Temperature!;
                    map["Q46"] = (w, dto, esDto, ws, reportNo) => w!.Ballast!;
                    map["P47"] = (w, dto, esDto, ws, reportNo) => w!.DryProcedure!;
                    map["Y47"] = (w, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(w.Iron!) ? "/" : w.IronMethod!;
                    map["A48"] = (w, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) ? "-" : w.SpecialCareInstruction!;
                }
                return map;
            },
            ["Attachment Strength"] = (w, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (w, dto, esDto, ws, reportNo) => reportNo,
            },
            ["Moisture Management"] = (w, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (w, dto, esDto, ws, reportNo) => reportNo;
                map["M48"] = (w, dto, esDto, ws, reportNo) => reportNo;
                if (string.IsNullOrEmpty(w.Sensitive) == false)
                {
                    map["L93"] = (w, dto, esDto, ws, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal";
                }
                else
                {
                    map["G88"] = (w, dto, esDto, ws, reportNo) => w!.WashingProcedure!;
                    map["AK88"] = (w, dto, esDto, ws, reportNo) => w!.Temperature!;
                    map["Q89"] = (w, dto, esDto, ws, reportNo) => w!.Ballast!;
                    map["L90"] = (w, dto, esDto, ws, reportNo) => w!.DryProcedure!;
                    map["U90"] = (w, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(w.Iron!) ? "/" : w.IronMethod!;
                    map["A91"] = (w, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) ? "-" : w.SpecialCareInstruction!;
                }
                return map;
            },
            ["Snagging Resistance"] = (w, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (w, dto, esDto, ws, reportNo) => reportNo,
            },
            ["Mass per Unit Length"] = (w, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["J1"] = (w, dto, esDto, ws, reportNo) => reportNo,
            },
            ["Fabric Width"] = (w, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (w, dto, esDto, ws, reportNo) => reportNo,
            },
            ["Hydrostatic Head Test"] = (w, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (w, dto, esDto, ws, reportNo) => reportNo;
                if (string.IsNullOrEmpty(w.Sensitive) == false)
                {
                    map["L41"] = (w, dto, esDto, ws, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal";
                }
                else
                {
                    map["G36"] = (w, dto, esDto, ws, reportNo) => w!.WashingProcedure!;
                    map["AJ36"] = (w, dto, esDto, ws, reportNo) => w!.Temperature!;
                    map["Q37"] = (w, dto, esDto, ws, reportNo) => w!.Ballast!;
                    map["P38"] = (w, dto, esDto, ws, reportNo) => w!.DryProcedure!;
                    map["Y38"] = (w, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(w.Iron!) ? "/" : w.IronMethod!;
                    map["A39"] = (w, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) ? "-" : w.SpecialCareInstruction!;
                }
                return map;
            },
            ["Air Permeability of Textile Fabrics"] = (w, dto, esDto, ws, reportNo) =>
            {
                var map = new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>();
                map["M1"] = (w, dto, esDto, ws, reportNo) => reportNo;
                map["A3"] = (w, dto, esDto, ws, reportNo) => dto.Standard!;
                map["F5"] = (w, dto, esDto, ws, reportNo) => "100";
                map["E6"] = (w, dto, esDto, ws, reportNo) => "20";
                map["V8"] = (w, dto, esDto, ws, reportNo) => dto.Parameter!.Contains("500") ? "2000revs" : "4000revs";
                if (string.IsNullOrEmpty(w.Sensitive) == false)
                {
                    map["L34"] = (w, dto, esDto, ws, reportNo) => w!.Sensitive == "Y" ? "Sensitive" : "Normal";
                }
                else
                {
                    map["G29"] = (w, dto, esDto, ws, reportNo) => w!.WashingProcedure!;
                    map["AJ29"] = (w, dto, esDto, ws, reportNo) => w!.Temperature!;
                    map["Q30"] = (w, dto, esDto, ws, reportNo) => w!.Ballast!;
                    map["P31"] = (w, dto, esDto, ws, reportNo) => w!.DryProcedure!;
                    map["Y31"] = (w, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(w.Iron!) ? "/" : w.IronMethod!;
                    map["A32"] = (w, dto, esDto, ws, reportNo) => string.IsNullOrEmpty(w.SpecialCareInstruction!) ? "-" : w.SpecialCareInstruction!;
                }
                return map;
            },
            ["Yarn Count"] = (w, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (w, dto, esDto, ws, reportNo) => reportNo,
                ["A3"]= (w, dto, esDto, ws, reportNo) => dto.Standard!,
            },
            ["Course and Wales"] = (w, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (w, dto, esDto, ws, reportNo) => reportNo,
            },
            ["Ends & Picks"] = (w, dto, esDto, ws, reportNo) => new Dictionary<string, Func<WetParameterIso, CheckListDto, ExcelSubmitDto, ExcelWorksheet, string, string>>
            {
                ["M1"] = (w, dto, esDto, ws, reportNo) => reportNo,
            },
        };



        private static readonly Dictionary<string, int> OffsetRule = new()
        {
            ["Fastness to Perspiration"] = 6,
            ["WIRA Steam Stability"] = 3,
            ["Stability to Washing"] = 4,
            ["Stability to Dry Cleaning"] = 4,
            ["Abrasion Home"] = 1,
            ["Extension and Modulus"] = 2,
            ["Extension and Recovery"] = 2,
            ["Accelerotor Pile Loss"] = 2,
            ["Hydrostatic Head Test"] = 3

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
            if (afmap != null && afmap.Length > 0
               && (itemName == "Stability to Washing" || itemName == "Stability to Dry Cleaning")
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



            if (itemName == "Stability to Washing" || itemName == "Stability to Dry Cleaning" && !sampleDescription.Contains("Fabric")) offset = 0;
            if (itemName.Contains("Appearance")||itemName== "Moisture Management")
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