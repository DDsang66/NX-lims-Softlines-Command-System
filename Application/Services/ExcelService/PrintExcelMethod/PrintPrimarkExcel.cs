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
using System.Linq;
using DocumentFormat.OpenXml.Office2010.CustomUI;
using SkiaSharp;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Drawing;



namespace NX_lims_Softlines_Command_System.Application.Services.ExcelService.PrintExcelMethod
{
    public sealed class PrintPrimarkExcel : IPrintExcelStrategy
    {
        private readonly LabDbContextSec _db;
        public PrintPrimarkExcel(LabDbContextSec db)
        {
            _db = db;
        }

        //关于excel的打印流程：
        //首先先扩展项目，例如单克重
        //针对每一个项目，获取其所有样本的参数包
        //然后对参数包内的样本遍历，比较参数并进行测点归类
        //当然后续衍生的测点也在同一个组，按照原生测点的归类去分
        //扩展测点
        //扩展后的测点按分类放入测点组
        //根据当前循环的项目和测点描述值找出要处理的excel模板
        //对excel模板进行处理，按照测点组的个数，测点的总个数结合excel的对应项目的测点容量进行sheet拓展
        //按照每个测点组结合excel的测点容量进行分隔测点，切片写入
        //最后借助字典把每个测点组的参数对应写入excel
        //当前循环结束，完成一个测试项目的打印，开始下一个项目的打印


        //参数包
        public class ParameterBag
        {
            public WetParameterIso? WetParam { get; set; }
            public NormalParameter? NormalParam { get; set; }
            public string? Type { get; set; }
        }

        //测点名称扩展
        public class ExpandedPoint
        {
            public string OriginalSample { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
            public string? AfterWash { get; set; }
        }

        // 新测点组结构：包含原始测点和其扩展
        public class SampleGroup
        {
            public string Signature { get; set; } = string.Empty;           // 参数签名
            public List<OriginalPoint> Points { get; set; } = new();        // 原始测点列表
        }

        public class OriginalPoint
        {
            public string Code { get; set; } = string.Empty;                // 原始测点名：A 或 A-main Fabric
            public ParameterBag Bag { get; set; } = new();                  // 该测点的参数包
            public List<ExpandedPoint> Expanded { get; set; } = new();      // 扩展后的测点列表
            public int[]? AfterWashMap { get; set; }                        // 该测点独有的水洗映射
        }



        /// <summary>
        /// 主方法
        /// </summary>
        /// <param name="esDto"></param>
        /// <param name="pkgWet"></param>
        /// <param name="pkgPhy"></param>
        public void PrintJsonData(ExcelSubmitDto esDto, ExcelPackage pkgWet, ExcelPackage pkgPhy)
        {
            if (esDto.NewSelectedRows!.FirstOrDefault(row => row.itemName == "Mass per Unit Area") == null) 
            {
                foreach (var row in esDto.NewSelectedRows!)
                {
                    var sampleList = row.samples!.Split(',').Select(s => s.Trim()).ToList();

                    var needMass = new[] { "Seam Slippage", "Seam Strength", "Tear Strength", "Tensile Strength",
        "Martindale Abrasion", "Back Pocket Application Strength", "Belt Loop Application Strength" }
                        .Contains(row.itemName);

                    if (!needMass) continue;

                    esDto.NewSelectedRows.Add(new NewSelectedRows
                    {
                        itemName = "Mass per Unit Area",
                        standards = "BS EN 12127:1998",
                        parameters = sampleList.Select(s => new Params
                        {
                            sample = s,
                            normalParam = "Single unit weight"
                        }).ToList(),
                        types = "Physics",
                        samples = row.samples,
                    });
                    break;
                }//单克重拓展
            }

            //主逻辑循环
            int groupIndex = 0;// 将groupIndex移到项目循环外部，使其在所有项目中持续递增

            foreach (var row in esDto.NewSelectedRows!)
            {
                //用于获取测点组索引，方便sheet命名

                var pkg = row.types == "Wet" ? pkgWet : pkgPhy;

                var groups = BuildGroupsWithExpansion(row, esDto.ReportNumber!);

                foreach (var group in groups)
                {
                    //组内代表点（第一个）
                    var representative = group.Points.First();
                    //获取水洗映射
                    var afMap = group.Points.FirstOrDefault()?.AfterWashMap;

                    //获取描述值（如有需要）
                    var descValue = GetDescValue(representative.Code, "State", esDto);

                    //1选择模板
                    var selector = new TemplateSelector(TemplateSheetNames, TemplateSheetNamesNormal);

                    var templateName = selector.GetTemplateName(row.itemName!, descValue!);

                    templateName = SelectTemplate(row.itemName!, row.standards!, templateName);

                    //在当前测点组获取模板Sheet
                    var template = pkg.Workbook.Worksheets[templateName];

                    //2计算容量和Sheet数
                    var allDisplayNames = group.Points.SelectMany(p => p.Expanded.Select(e => e.DisplayName)).ToList();             //当前组的所有测点

                    var cellAddrs = GetCellAddresses(row.itemName!, row.standards!, descValue);                                                      //先去拿到单元格地址

                    var capacity = GetCapacity(row.itemName!, row.standards!, cellAddrs.Length, descValue);                          //计算容量

                    var sheetCnt = (int)Math.Ceiling(allDisplayNames.Count / (double)capacity);

                    var sheets = new List<ExcelWorksheet> { template };
                    for (int i = 0; i < sheetCnt; i++)
                    {
                        string name = $"{templateName}_G{groupIndex}_{i + 1}";  // 加组索引，避免多组命名冲突
                        sheets.Add(pkg.Workbook.Worksheets.Any(ws => ws.Name == name)
                            ? pkg.Workbook.Worksheets[name]
                            : pkg.Workbook.Worksheets.Copy(templateName, name));
                    }

                    //3切片
                    var slices = BuildSlices(allDisplayNames, capacity);

                    //每张Sheet填一个切片
                    for (int idx = 0; idx < slices.Count; idx++)
                    {
                        FillSlice(sheets[idx], slices[idx], group, row, esDto.ReportNumber!, esDto, afMap);
                    }

                    groupIndex++;//用于获取测点组索引，方便sheet命名
                }
                groupIndex = 0;//重置组索引，以便下一个项目重新开始
            } 
            pkgWet.Save();
            pkgPhy.Save();
        }

        /// <summary>
        /// 填充方法
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="slice"></param>
        /// <param name="group"></param>
        /// <param name="row"></param>
        /// <param name="reportNo"></param>
        /// <param name="esDto"></param>
        private void FillSlice(
            ExcelWorksheet sheet,
            List<string> slice, 
            SampleGroup group, 
            NewSelectedRows row, 
            string reportNo, 
            ExcelSubmitDto esDto,
            int[]? afMap)
        {
            // 获取单元格地址
            var descValue = GetDescValue(group.Points[0].Code, "State", esDto);

            var cellAddrs = GetCellAddresses(row.itemName!, row.standards!, descValue);

            // 写入测点
            WriteSamples(sheet, slice, afMap, cellAddrs, row.itemName!, descValue!, row.standards!);

            // 写入参数（借助字典）
            var representative = group.Points.First();

            FillParameters(sheet, representative.Bag, row, esDto, afMap, group.Points[0].Code, group);
        }

        /// <summary>
        /// 填充参数
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="bag"></param>
        /// <param name="row"></param>
        /// <param name="esDto"></param>
        private void FillParameters(
            ExcelWorksheet sheet, 
            ParameterBag bag,
            NewSelectedRows row, 
            ExcelSubmitDto esDto, 
            int[]? afMap,string sample, 
            SampleGroup group)
        {
            // 1. 填测点名称（在 FillSlice 已做）

            // 2. 填 AfterWash（如有）
            if (AfterWashCellMapper.ContainsKey(row.itemName!))
            {
                var descValue = GetDescValue(group.Points[0].Code, "State", esDto);

                var afterWashAddrs = AfterWashCellMapper[row.itemName!](row.itemName!, row.standards!, "");

                WriteAfterWash(sheet, afMap, afterWashAddrs, row.itemName!, row.standards!, descValue!);
            }

            // 3. 填参数（ExtraMap）
            var extraMap = bag.Type == "Wet"
           ? BulidWetExtraMap!.GetValueOrDefault(row.itemName, (wp, np, row, esDto,sample)
                => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>())(bag.WetParam!, bag.NormalParam!, row, esDto,sample)
           : BulidPhyExtraMap!.GetValueOrDefault(row.itemName, (wp, np, row, esDto,sample)
                => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>())(bag.WetParam!, bag.NormalParam!, row, esDto, sample);

            foreach (var kv in extraMap)
            {
                sheet.Cells[kv.Key].Value = kv.Value(bag.WetParam!, bag.NormalParam!, row, esDto,sample!);
            }
        }

        /// <summary>
        /// 测点参数包，只和原测点有关
        /// </summary>
        /// <param name="row"></param>
        /// <param name="reportNo"></param>
        /// <param name="buyer"></param>
        /// <returns></returns>
        private Dictionary<string, ParameterBag> LoadParamBagsByItemName(NewSelectedRows row, string reportNo,string buyer)
        {
            var bags = new Dictionary<string, ParameterBag>();

            // 获取所有样本
            var samples = row.samples!.Split(',').Select(s => s.Trim()).ToArray();

            // 批量查库
            var wetList = _db.WetParameterIsos
                .Where(p => p.ContactItem == row.itemName && p.ReportNumber == reportNo && p.ContactBuyer == buyer)
                .ToList();

            var norList = _db.NormalParameters
                .Where(p => p.ContactItem == row.itemName && p.ReportNumber == reportNo && p.ContactBuyer == buyer)
                .ToList();

            // 为每个样本构造 Bag
            foreach (var sample in samples)
            {
                var wp = wetList.FirstOrDefault(w => w.ContactSample == sample);
                var np = norList.FirstOrDefault(n => n.ContactSample == sample);

                bags[sample] = new ParameterBag
                {
                    WetParam = wp,
                    NormalParam = np,
                    Type = row.types!,
                };
            }

            return bags;
        }

        /// <summary>
        ///根据测点获取描述值
        /// </summary>
        /// <param name="sample"></param>
        /// <param name="propertyName"></param>
        /// <param name="esDto"></param>
        /// <returns></returns>
        private static string? GetDescValue(
            string sample,
            string propertyName,
            ExcelSubmitDto esDto)
        {
            return esDto.SampleDescripBoundSingleDto
                ?.FirstOrDefault(x => x.sample == sample)
                ?.description
                ?.FirstOrDefault(d => d.propertyName == propertyName)
            ?.value;
        }

        /// <summary>
        /// 比较参数包是否一致，进行测点归类
        /// </summary>
        /// <param name="bags"></param>
        /// <returns></returns>
        private List<SampleGroup> GroupSamplesByParameters(Dictionary<string, ParameterBag> bags)
        {
            var groups = new List<SampleGroup>();

            foreach (var (sample, bag) in bags)
            {
                // 生成参数签名（用于比较是否一致）
                var signature = GenerateSignature(bag);

                // 查找已有相同签名的组
                var existingGroup = groups.FirstOrDefault(g => g.Signature == signature);

                if (existingGroup != null)
                {
                    existingGroup.Points.Add(new OriginalPoint { Code = sample, Bag = bag });
                }
                else
                {
                    groups.Add(new SampleGroup
                    {
                        Signature = signature,
                        Points = new List<OriginalPoint> { new OriginalPoint { Code = sample, Bag = bag } },
                    });
                }
            }

            return groups;
        }

        /// <summary>
        /// 参数签名生成（根据实际字段调整）
        /// </summary>
        /// <param name="bag"></param>
        /// <returns></returns>
        private string GenerateSignature(ParameterBag bag)
        {
            var parts = new List<string> { bag.Type! };

            // WetParam 部分
            if (bag.WetParam != null)
            {
                var wetValues = bag.WetParam.GetType()
                    .GetProperties()
                    .Where(p => p.Name != "ContactSample"&& p.Name != "ParamId")
                    .Select(p => p.GetValue(bag.WetParam)?.ToString() ?? "null");
                parts.Add($"Wet[{string.Join(",", wetValues)}]");
            }

            // NormalParam 部分
            if (bag.NormalParam != null)
            {
                var normalValues = bag.NormalParam.GetType()
                    .GetProperties()
                      .Where(p => p.Name != "ContactSample" && p.Name != "ParamId")
                    .Select(p => p.GetValue(bag.NormalParam)?.ToString() ?? "null");
                parts.Add($"Normal[{string.Join(",", normalValues)}]");
            }

            return string.Join("|", parts);
        }

        private List<SampleGroup> BuildGroupsWithExpansion(NewSelectedRows row, string reportNo)
        {
            // 1. 原始测点组
            var bags = LoadParamBagsByItemName(row, reportNo, "Primark");
            var groups = GroupSamplesByParameters(bags);

            // 2. 分别扩展后合并
            foreach (var group in groups)
            {
                foreach (var point in group.Points)
                {
                    // 2.1 项目特殊扩展（可能没有）
                    var specialNames = ExpandSinglePointNames(point, row);

                    // 2.2 水洗扩展（可能没有）
                    var washResult = ExpandWashSamples(
                        row.itemName!,
                        row.standards!,
                        point.Code,
                        reportNo,
                        point.Bag.WetParam as WetParameterIso
                    );
                    var washNames = washResult.samples;
                    if (row.itemName == "Spirality" && row.standards == "PM01") washNames = [];
                    var afMap = washResult.afterWashMap;
                    // 2.3 合并填入
                    point.Expanded = MergeTwoExpansions(specialNames, washNames, point.Code);
                    point.AfterWashMap = afMap;
                }
            }

            return groups;
        }

        // 只扩展当前point，返回名字列表
        private List<string> ExpandSinglePointNames(OriginalPoint point, NewSelectedRows row)
        {
            return row.itemName switch
            {
                "Spirality" when row.standards == "PM01"
                    => new[] { "5", "23", "32", "45" }.Select(w => $"{point.Code} × {w}").ToList(),

                "TS Board Fit" when row.standards == "PM01"
                    => new List<string> { point.Code }
                        .Concat(new[] { "5", "23", "32", "45" }.Select(w => $"{point.Code} After {w} Washes"))
                        .ToList(),

                _ => new List<string> { point.Code }
            };
        }

        /// <summary>
        /// 根据水洗遍数标准拓展测点个数和样式
        /// </summary>
        /// <param name="itemName">项目名称</param>
        /// <param name="standard">标准</param>
        /// <param name="sampleCode">样本代码</param>
        /// <param name="reportNo">报告号</param>
        /// <param name="wp">湿处理参数</param>
        /// <returns>扩展后的样本名数组和水洗映射</returns>
        private (string[] samples, int[]? afterWashMap) ExpandWashSamples(
            string itemName,
            string standard,
            string sampleCode,
            string reportNo,
            WetParameterIso? wp)
        {
            // 判断是否需要水洗扩展
            var needExpand = itemName is "Stability to Dry Cleaning" or "Stability to Washing"
                or "Appearance-Common" or "Security of Attachment(Wash)" or "Easycare/Non-Iron"
                || (itemName == "Appearance" && standard != "PM01");

            // 不需要扩展，返回原始样本
            if (!needExpand)
                return (new[] { sampleCode }, null);

            // 防null
            wp ??= new WetParameterIso();

            // 调用SampleNumCounter扩展样本名
            var expanded = SampleNumCounter.GetSample(sampleCode, wp.AfterWash, wp.Iron, wp.IronMethod);

            // 生成水洗次数映射
            var afterWashMap = SampleNumCounter.ExpandWashNumbers(expanded!, wp.AfterWash!, wp.Iron);

            return (expanded!, afterWashMap);
        }

        // 合并两种扩展（笛卡尔积：特殊扩展 × 水洗扩展）
        private List<ExpandedPoint> MergeTwoExpansions(
            List<string> specialNames, 
            string[] washNames, 
            string originalCode)
        {
            var result = new List<ExpandedPoint>();
            
            // 情况1：只有特殊扩展，无水洗扩展
            if (washNames == null || washNames.Length == 0)
            {
                foreach (var name in specialNames)
                {
                    result.Add(new ExpandedPoint
                    {
                        DisplayName = name,
                        OriginalSample = originalCode,
                        AfterWash = null
                    });
                }
                return result;
            }

            // 情况2：只有水洗扩展，无特殊扩展（specialNames只有原始名）
            if (specialNames.Count == 1 && specialNames[0] == originalCode)
            {
                for (int i = 0; i < washNames.Length; i++)
                {
                    result.Add(new ExpandedPoint
                    {
                        DisplayName = washNames[i],
                        OriginalSample = originalCode,
                        AfterWash = washNames[i].Contains("After")
                            ? washNames[i].Split(' ').Last()
                            : null
                    });
                }
                return result;
            }

            // 情况3：两者都有，笛卡尔积（特殊扩展 × 水洗）
            foreach (var special in specialNames)
            {
                foreach (var wash in washNames)
                {
                    result.Add(new ExpandedPoint
                    {
                        DisplayName = $"{special} - {wash}", // 或根据业务调整格式
                        OriginalSample = originalCode,
                        AfterWash = wash.Contains("After")
                            ? wash.Split(' ').Last()
                            : null
                    });
                }
            }

            return result;
        }


        /// <summary>
        /// 额外模板选择逻辑
        /// </summary>
        /// <param name="itemName"></param>
        /// <param name="standard"></param>
        /// <param name="descValue"></param>
        /// <returns></returns>
        private string SelectTemplate(
            string itemName, 
            string standard, 
            string templateName)
        {
            // 特殊项目多标准映射
            return itemName switch
            {
                "Physical & Mechanical" => standard switch
                {
                    _ when standard.Contains("EN 71-1:2014+A1:2018 8.4") => "Attachment Strength",
                    _ when standard.Contains("ASTM F963-23") => "ASTM F963-23",
                    _ => "Physical & Mechanical"
                },
                "Torque & Tension" => standard switch
                {
                    _ when standard.Contains("16 CFR 1500.51-53") => "Torque&Tension",
                    _ when standard.Contains("EN 71-1:2024+A1:2018") => "Torque&Tension-EN 71",
                    _ => "Torque & Tension"
                },
                _ => templateName // 或根据描述值组合
            };
        }

        /// <summary>
        /// 特殊项目的容量获取
        /// </summary>
        /// <param name="itemName"></param>
        /// <param name="standard"></param>
        /// <param name="cellCount"></param>
        /// <returns></returns>
        private int GetCapacity(
            string itemName, 
            string standard,
            int cellCount,
            string descValue)
        {
            // 固定容量的特殊项目
            if (itemName == "Colour Fastness to Hot Pressing") return 3;

            if (itemName is "Appearance" or "Appearance-Common" or "Dimensional Stability"
                         or "Easycare/Non-Iron") return 1;

            if (itemName == "Stability to Washing" && !descValue.Contains("Fabric")) return 1;

            if (itemName == "TS Board Fit") return 2;

            var offset = OffsetRule.GetValueOrDefault(itemName, 0);
            
            cellCount = offset > 0 ? cellCount / 2 : cellCount;
            // 默认按单元格数
            return cellCount;
        }

        // 切片方法
        private List<List<string>> BuildSlices(List<string> names, int capacity)
        {
            var slices = new List<List<string>>();
            for (int i = 0; i < names.Count; i += capacity)
            {
                slices.Add(names.Skip(i).Take(capacity).ToList());
            }
            return slices;
        }

        // 获取单元格地址方法
        private string[] GetCellAddresses(string itemName, string standard, string? sampleDescription)
        {
            if (!CellMapper.ContainsKey(itemName))
                throw new ArgumentException($"未找到 {itemName} 的单元格映射配置");

            return CellMapper[itemName](itemName, standard, sampleDescription ?? "");
        }
        /// <summary>
        /// 测点写入方法
        /// </summary>
        /// <param name="ws"></param>
        /// <param name="slice"></param>
        /// <param name="afmap"></param>
        /// <param name="cellAddrs"></param>
        /// <param name="itemName"></param>
        /// <param name="descValue"></param>
        /// <param name="standard"></param>
        private void WriteSamples(
            ExcelWorksheet ws,
            List<string> slice,
            int[]? afmap,
            string[] cellAddrs,
            string itemName,
            string descValue,
            string standard)
        {
            int offset = OffsetRule.GetValueOrDefault(itemName, 0);
            if ((itemName == "Dimensional Stability" || itemName == "Stability to Washing") && !descValue.Contains("Fabric")) offset = 0;

            if (itemName == "Appearance"
                || itemName == "Dimensional and Bra Wire Casing Stability"
                || itemName == "Appearance-Common" || itemName == "Dimensional Stability")
            {
                for (int i = 0; i < cellAddrs.Length; i++)
                {
                    ws.Cells[cellAddrs[i]].Value = slice[0];
                }
            }
            else if (itemName == "Colour Fastness to Hot Pressing")
            {
                for (int i = 0; i < slice.Count(); i++)
                {
                    ws.Cells[cellAddrs[i]].Value = slice[i];
                    ws.Cells[cellAddrs[i + 3]].Value = slice[i];
                    ws.Cells[cellAddrs[i + 6]].Value = slice[i];
                }
            }
            else
            {
                for (int i = 0; i < slice.Count(); i++)
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

        /// <summary>
        /// 统一写入 AfterWash 数据到指定单元格
        /// </summary>
        /// <param name="ws">工作表</param>
        /// <param name="afmap">数据源</param>
        /// <param name="AfterWashCellAddrs">目标单元格地址</param>
        /// <param name="itemName">项目名</param>
        /// <param name="standard">标准</param>
        /// <param name="sampleDescription">样本描述（用于 Stability to Washing 过滤）</param>
        private static void WriteAfterWash(
            ExcelWorksheet ws,
            int[]? afmap,
            string[]? AfterWashCellAddrs,
            string itemName,
            string standard,
            string sampleDescription)
        {
            if (afmap == null || afmap.Length == 0 || AfterWashCellAddrs == null || AfterWashCellAddrs.Length == 0)
                return;

            // 策略：返回本次要写入的整数序列
            IEnumerable<int> data = itemName switch
            {
                "Appearance-Common" when standard != "PM01"
                    => Enumerable.Repeat(afmap[0], AfterWashCellAddrs.Length),

                "Stability to Washing" when !sampleDescription.Contains("Fabric")
                    => Enumerable.Repeat(afmap[0], AfterWashCellAddrs.Length),

                _ => afmap
            };

            // 按序列写入，超出地址数组长度则截断
            int idx = 0;
            foreach (var value in data)
            {
                if (idx >= AfterWashCellAddrs.Length) break;
                ws.Cells[AfterWashCellAddrs[idx++]].Value = value;
            }
        }

        /// <summary>
        /// 测点填写偏移量
        /// </summary>
        private static readonly Dictionary<string, int> OffsetRule = new()
        {
            ["Colour Fastness to Perspiration"] = 6,
            ["Stability to Washing"] = 4,
            ["Stability to Dry Cleaning"] = 4,
            ["Colour Fastness to Non Chlorine Bleach"] = 6,
            ["Shower Resistant Claims Spray Rating"] = 3,
            ["Absorbency of Textiles"] = 6,
            ["Waterproof Claims Hydrostatic Head"] = 2
        };
        /// <summary>
        /// sheet名称
        /// </summary>
        private static readonly Dictionary<string, string> TemplateSheetNamesNormal = new()
        {
            ["Abrasion of Knitted Footwear Garments - Modified Martindale"] = "Abrasion",
            ["Absorbency of Textiles"] = "Absorbency",
            ["Accelerotor"] = "Accelerotor",
            ["Back Pocket Application Strength"] = "PM07PM08",
            ["Belt Loop Application Strength"] = "PM07PM08",
            ["Chenille Pile Loss"] = "PM06",
            ["Elastic Extension and Modulus Test"] = "PM23&AR(TABER)",
            ["EU Security of Attachment on Children's Clothing"] = "Attachment Strength",
            ["Fibre Proof Properties"] = "Fibre Proof Properties",
            ["Fibre Shedding"] = "PM03PM05",
            ["Martindale Abrasion"] = "Abrasion",
            ["Martindale Pilling"] = "Pilling Resistance",
            ["Mass per Unit Area"] = "Weight",
            ["Nap Stability"] = "PM03PM05",
            ["Peel Bond"] = "Peel Bond",
            ["Pile Retention"] = "PM03PM05",
            ["Quick Dry"] = "DryingRate",
            ["Residual Elongation"] = "Elongation",
            ["Residual Elongation SHAPEWEAR"] = "Elongation",
            ["Security of Attachment"] = "Attachment Strength",
            ["Security of Attachment Buttons"] = "Attachment Strength",
            ["Security of Attachment Mechanically Applied Fasteners"] = "Attachment Strength",
            ["Sharp Edges Restrctions"] = "Torque&Tension",
            ["Sharp Point Restrctions"] = "Torque&Tension",
            ["Small Parts Restrictions"] = "Torque&Tension",
            ["Shower Resistant Claims Spray Rating"] = "WaterRepellency",
            ["Tear Strength"] = "Tearing Strength",
            ["Tensile Strength"] = "Tensile Strength",
            ["Unrecovered Elongation"] = "Elongation",
            ["Waterproof Claims Hydrostatic Head"] = "Hydrostatichead",
            ["Wind Resistant Claims Air Permeability"] = "Air Permeability",
            ["Zip Fasteners"] = "ZipperStrength",
            ["Vertical Wicking of Textiles"] = "Wicking",

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
        /// <summary>
        /// sheet名称
        /// </summary>
        private static readonly Dictionary<string, Dictionary<string[], string>> TemplateSheetNames = new()
        {
            [("Seam Slippage")] = new Dictionary<string[], string>
            {
                {new[] { "Fabric" }, "Seam Slippage&Strength" },
                {new[] { "Garment" },"Seam Slippage&Strength-G"},
            },
            [("Seam Strength")] = new Dictionary<string[], string>
            {
                {new[] { "Fabric" }, "Seam Slippage&Strength" },
                {new[] { "Garment","Knit" },"Bursting Strength-G"},
                {new[] { "Garment" },"Seam Slippage&Strength-G"}
            },
            [("Bursting Strength")] = new Dictionary<string[], string>
            {
                {new[] { "Fabric" }, "Bursting Strength" },
                {new[] { "Garment" },"Bursting Strength-G"},
            },
            [("Physical & Mechanical")] = new Dictionary<string[], string>
            {
                {new[] { "EN 71-1:2014+A1:2018 8.4" }, "Attachment Strength" },
            },
            [("Physical & Mechanical")] = new Dictionary<string[], string>
            {
                {new[] { "ASTM F963-23" }, "ASTM F963-23" },
            },
            [("Torque & Tension")] = new Dictionary<string[], string>
            {
                {new[] { "16 CFR 1500.51-53" }, "Torque&Tension" },
            },
            [("Torque & Tension")] = new Dictionary<string[], string>
            {
                {new[] { "EN 71-1:2024+A1:2018" }, "Torque&Tension-EN 71" },
            },
            [("Spirality")] = new Dictionary<string[], string>
            {
                {new[] { "Fabric" }, "Spirality-F" },
                {new[] { "Garment" }, "Spirality-G" },
            },
            [("Dimensional Stability")] = new Dictionary<string[], string>
            {
                {new[] { "Fabric" }, "PM01Washing-F" },
                {new[] { "Garment" }, "PM01Washing-G" },
                {new[] { "Socks" }, "PM01Washing-Acc" },
                {new[] { "Gloves" }, "PM01Washing-Acc" },
                {new[] { "Cap" }, "PM01Washing-Acc" },
            },
            [("Stability to Washing")] = new Dictionary<string[], string>
            {
                {new[] { "Fabric" }, "DStoWashing-F" },
                {new[] { "Garment" }, "DStoWashing-G" },
                {new[] { "Socks" }, "DStoWashing-Acc" },
                {new[] { "Gloves" }, "DStoWashing-Acc" },
                {new[] { "Cap" }, "DStoWashing-Acc" },
            },
        };
        /// <summary>
        /// 测点单元格
        /// </summary>
        private static readonly Dictionary<string, Func<string, string, string, string[]>> CellMapper = new()
        {
            ["Abrasion of Knitted Footwear Garments - Modified Martindale"] = (n, m, l) => ExcelPrimarkMapper.MapAbrasion(m),
            ["Absorbency of Textiles"] = (n, m, l) => ExcelPrimarkMapper.MapAbsorbency(),
            ["Accelerotor"] = (n, m, l) => ExcelPrimarkMapper.MapAccelerotor(),
            ["Back Pocket Application Strength"] = (n, m, l) => ExcelPrimarkMapper.MapPM07PM08(m),
            ["Belt Loop Application Strength"] = (n, m, l) => ExcelPrimarkMapper.MapPM07PM08(m),
            ["Chenille Pile Loss"] = (n, m, l) => ExcelPrimarkMapper.MapPM06(),
            ["Elastic Extension and Modulus Test"] = (n, m, l) => ExcelPrimarkMapper.MapPM23TABER(m),
            ["EU Security of Attachment on Children's Clothing"] = (n, m, l) => ExcelPrimarkMapper.MapAttachmentStrength(),
            ["Fibre Proof Properties"] = (n, m, l) => ExcelPrimarkMapper.MapFibreProof(),
            ["Fibre Shedding"] = (n, m, l) => ExcelPrimarkMapper.MapPM03PM05(m),
            ["Martindale Abrasion"] = (n, m, l) => ExcelPrimarkMapper.MapAbrasion(m),
            ["Martindale Pilling"] = (n, m, l) => ExcelPrimarkMapper.MapPilling(),
            ["Mass per Unit Area"] = (n, m, l) => ExcelPrimarkMapper.MapWeight(),
            ["Nap Stability"] = (n, m, l) => ExcelPrimarkMapper.MapPM03PM05(m),
            ["Peel Bond"] = (n, m, l) => ExcelPrimarkMapper.MapPeelBond(),
            ["Pile Retention"] = (n, m, l) => ExcelPrimarkMapper.MapPM03PM05(m),
            ["Quick Dry"] = (n, m, l) => ExcelPrimarkMapper.MapDryRate(),
            ["Residual Elongation"] = (n, m, l) => ExcelPrimarkMapper.MapElastic(),
            ["Residual Elongation SHAPEWEAR"] = (n, m, l) => ExcelPrimarkMapper.MapElastic(),
            ["Security of Attachment"] = (n, m, l) => ExcelPrimarkMapper.MapAttachmentStrength(),
            ["Security of Attachment Buttons"] = (n, m, l) => ExcelPrimarkMapper.MapAttachmentStrength(),
            ["Security of Attachment Mechanically Applied Fasteners"] = (n, m, l) => ExcelPrimarkMapper.MapAttachmentStrength(),
            ["Sharp Edges Restrctions"] = (n, m, l) => ExcelPrimarkMapper.MapTorqueTension(m),
            ["Sharp Point Restrctions"] = (n, m, l) => ExcelPrimarkMapper.MapTorqueTension(m),
            ["Small Parts Restrictions"] = (n, m, l) => ExcelPrimarkMapper.MapTorqueTension(m),
            ["Shower Resistant Claims Spray Rating"] = (n, m, l) => ExcelPrimarkMapper.MapRepellency(l),
            ["Tear Strength"] = (n, m, l) => ExcelPrimarkMapper.MapTear(),
            ["Tensile Strength"] = (n, m, l) => ExcelPrimarkMapper.MapTensile(),
            ["Unrecovered Elongation"] = (n, m, l) => ExcelPrimarkMapper.MapElastic(),
            ["Waterproof Claims Hydrostatic Head"] = (n, m, l) => ExcelPrimarkMapper.MapHydroatatic(),
            ["Wind Resistant Claims Air Permeability"] = (n, m, l) => ExcelPrimarkMapper.MapAir(),
            ["Zip Fasteners"] = (n, m, l) => ExcelPrimarkMapper.MapZipper(),
            ["Vertical Wicking of Textiles"] = (n, m, l) => ExcelPrimarkMapper.MapWicking(),
            ["Bursting Strength"] = (n, m, l) => ExcelPrimarkMapper.MapBursting(l),
            ["Seam Slippage"] = (n, m, l) => ExcelPrimarkMapper.MapSlippageStrength(n, l),
            ["Seam Strength"] = (n, m, l) => ExcelPrimarkMapper.MapSlippageStrength(n, l),
            ["Physical & Mechanical"] = (n, m, l) => ExcelPrimarkMapper.MapPhysicalMechanical(m),
            ["Torque & Tension"] = (n, m, l) => ExcelPrimarkMapper.MapTorqueTension(m),


            ["Colour Fastness to Chlorinated Water"] = (n, m, l) => ExcelPrimarkMapper.MapSPC(n),
            ["Colour Fastness to Chlorine Bleach"] = (n, m, l) => ExcelPrimarkMapper.MapPB(n),
            ["Colour Fastness to Dry Cleaning"] = (n, m, l) => ExcelPrimarkMapper.MapYD(n),
            ["Colour Fastness to Hot Pressing"] = (n, m, l) => ExcelPrimarkMapper.MapSPC(n),
            ["Colour Fastness to Light"] = (n, m, l) => ExcelPrimarkMapper.MapWRLW(n),
            ["Colour Fastness to Non Chlorine Bleach"] = (n, m, l) => ExcelPrimarkMapper.MapPB(n),
            ["Colour Fastness to Perspiration"] = (n, m, l) => ExcelPrimarkMapper.MapPB(n),
            ["Colour Fastness to PVC Migration"] = (n, m, l) => ExcelPrimarkMapper.MapSeaWaterPVC(n),
            ["Colour Fastness to Rubbing"] = (n, m, l) => ExcelPrimarkMapper.MapWRLW(n),
            ["Colour Fastness to Saliva"] = (n, m, l) => ExcelPrimarkMapper.MapCFtoSalivaSweat(),
            ["Colour Fastness to Saliva and Perspiration"] = (n, m, l) => ExcelPrimarkMapper.MapCFtoSalivaSweat(),
            ["Colour Fastness to Sea Water"] = (n, m, l) => ExcelPrimarkMapper.MapSeaWaterPVC(n),
            ["Colour Fastness to Washing"] = (n, m, l) => ExcelPrimarkMapper.MapWRLW(n),
            ["Colour Fastness to Water"] = (n, m, l) => ExcelPrimarkMapper.MapWRLW(n),
            ["Dimensional and Bra Wire Casing Stability"] = (n, m, l) => ExcelPrimarkMapper.MapBra(),
            ["Dye Transfer in Storage"] = (n, m, l) => ExcelPrimarkMapper.MapCFtoTD(n),
            ["Easycare/Non-Iron"] = (n, m, l) => ExcelPrimarkMapper.MapCFtoEI(m),
            ["Phenolic Yellowing"] = (n, m, l) => ExcelPrimarkMapper.MapYD(n),
            ["Print / Motif / Flock Durability"] = (n, m, l) => ExcelPrimarkMapper.MapDurability(),
            ["Print Durability"] = (n, m, l) => ExcelPrimarkMapper.MapDurability(),
            ["Security of Attachment(Wash)"] = (n, m, l) => ExcelPrimarkMapper.MapAttachment(),
            ["Stability to Dry Cleaning"] = (n, m, l) => ExcelPrimarkMapper.MapStabilityToDryClean(),
            ["TS Board Fit"] = (n, m, l) => ExcelPrimarkMapper.MapCFtoTD(n),
            ["Appearance"] = (n, m, l) => ExcelPrimarkMapper.MapAppearance(m),
            ["Appearance-Common"] = (n, m, l) => ExcelPrimarkMapper.MapAppearance(m),
            ["Colour Change and Staining"] = (n, m, l) => ExcelPrimarkMapper.MapAppearance(m),
            ["Spirality"] = (n, m, l) => ExcelPrimarkMapper.MapSpirality(l),
            ["Dimensional Stability"] = (n, m, l) => ExcelPrimarkMapper.MapPM01(l),
            ["Stability to Washing"] = (n, m, l) => ExcelPrimarkMapper.MapStability(l),
        };
        /// <summary>
        /// 洗涤遍数
        /// </summary>
        private static readonly Dictionary<string, Func<string, string, string, string[]>> AfterWashCellMapper = new()
        {
            //["Dimensional Stability"] = (n, m, l) => ExcelPrimarkMapper.StabilityAf(l),
            ["Stability to Washing"] = (n, m, l) => ExcelPrimarkMapper.StabilityAf(l),
            ["Stability to Dry Cleaning"] = (n, m, l) => ExcelPrimarkMapper.DStoDCAf(),
            ["Print / Motif / Flock Durability"] = (n, m, l) => ExcelPrimarkMapper.DurabilityAf(),
            ["Print Durability"] = (n, m, l) => ExcelPrimarkMapper.DurabilityAf(),
            ["Security of Attachment(Wash)"] = (n, m, l) => ExcelPrimarkMapper.AttachmentAf(),
            ["Easycare/Non-Iron"] = (n, m, l) => ExcelPrimarkMapper.EasyCareAf(m),
            ["Appearance-Common"] = (n, m, l) => ExcelPrimarkMapper.AppearanceAf(),
        };
        /// <summary>
        /// wet参数单元格映射
        /// </summary>
        private static readonly Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string,
            Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>>> BulidWetExtraMap = new()
            {
                ["Colour Fastness to Chlorinated Water"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["H1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A27"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["E28"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("50mg/L") ? "50mg/L" : "20mg/L",
                },
                ["Colour Fastness to Washing"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["D1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["B4"] = (wp, np, row, esDto, sample) => wp.Program!,
                    ["E4"] = (wp, np, row, esDto, sample) => wp.Temperature!,
                    ["L5"] = (wp, np, row, esDto, sample) => wp.SteelBallNum.ToString()!,
                },
                ["Dimensional Stability"] = (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    if (GetDescValue(sample, "State", esDto)!.Contains("Fabric"))
                    {
                        map["BC1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                        map["AR3"] = (wp, np, row, esDto, sample) => "BS EN ISO 5077:2008/BS EN ISO 3759:2011/BS EN ISO 6330:2021";
                        map["AX4"] = (wp, np, row, esDto, sample) => wp.WashingProcedure!;
                        map["BX4"] = (wp, np, row, esDto, sample) => wp.Temperature!;
                        map["BF5"] = (wp, np, row, esDto, sample) => wp.Ballast!;
                        map["AR6"] = (wp, np, row, esDto, sample) => wp.Detergent!;
                        map["BM6"] = (wp, np, row, esDto, sample) => wp.DryProcedure!;
                        map["BV6"] = (wp, np, row, esDto, sample) => "/ Iron";
                        map["AR7"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                    }
                    else if (GetDescValue(sample, "State", esDto)!.Contains("Garment"))
                    {
                        map["P1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                        map["A3"] = (wp, np, row, esDto, sample) => "BS EN ISO 5077:2008/BS EN ISO 3759:2011/BS EN ISO 6330:2021";
                        map["I4"] = (wp, np, row, esDto, sample) => wp.WashingProcedure!;
                        map["AJ4"] = (wp, np, row, esDto, sample) => wp.Temperature!;
                        map["S5"] = (wp, np, row, esDto, sample) => wp.Ballast!;
                        map["A6"] = (wp, np, row, esDto, sample) => wp.Detergent!;
                        map["Y6"] = (wp, np, row, esDto, sample) => wp.DryProcedure!;
                        map["AH6"] = (wp, np, row, esDto, sample) => "/ Iron";
                        map["A7"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;

                        map["P52"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                        map["A54"] = (wp, np, row, esDto, sample) => "BS EN ISO 5077:2008/BS EN ISO 3759:2011/BS EN ISO 6330:2021";
                        map["I55"] = (wp, np, row, esDto, sample) => wp.WashingProcedure!;
                        map["AJ55"] = (wp, np, row, esDto, sample) => wp.Temperature!;
                        map["S56"] = (wp, np, row, esDto, sample) => wp.Ballast!;
                        map["A57"] = (wp, np, row, esDto, sample) => wp.Detergent!;
                        map["Y57"] = (wp, np, row, esDto, sample) => wp.DryProcedure!;
                        map["AH57"] = (wp, np, row, esDto, sample) => "/ Iron";
                        map["A58"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                    }
                    else if (GetDescValue(sample, "State", esDto)!.Contains("Cap")
                    || GetDescValue(sample, "State", esDto)!.Contains("Socks")
                    || GetDescValue(sample, "State", esDto)!.Contains("Gloves"))
                    {
                        map["N1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                        map["A3"] = (wp, np, row, esDto, sample) => "BS EN ISO 5077:2008/BS EN ISO 3759:2011/BS EN ISO 6330:2021";
                        map["G4"] = (wp, np, row, esDto, sample) => wp.WashingProcedure!;
                        map["AL4"] = (wp, np, row, esDto, sample) => wp.Temperature!;
                        map["R5"] = (wp, np, row, esDto, sample) => wp.Ballast!;
                        map["A6"] = (wp, np, row, esDto, sample) => wp.Detergent!;
                        map["Y6"] = (wp, np, row, esDto, sample) => wp.DryProcedure!;
                        map["AH6"] = (wp, np, row, esDto, sample) => "/ Iron";
                        map["A7"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;

                        map["N56"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                        map["A58"] = (wp, np, row, esDto, sample) => "BS EN ISO 5077:2008/BS EN ISO 3759:2011/BS EN ISO 6330:2021";
                        map["G59"] = (wp, np, row, esDto, sample) => wp.WashingProcedure!;
                        map["AL59"] = (wp, np, row, esDto, sample) => wp.Temperature!;
                        map["R60"] = (wp, np, row, esDto, sample) => wp.Ballast!;
                        map["A61"] = (wp, np, row, esDto, sample) => wp.Detergent!;
                        map["Y61"] = (wp, np, row, esDto, sample) => wp.DryProcedure!;
                        map["AH61"] = (wp, np, row, esDto, sample) => "/ Iron";
                        map["A62"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;

                    }
                    return map;
                },
                ["Stability to Washing"] = (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    if (GetDescValue(sample, "State", esDto)!.Contains("Fabric"))
                    {
                        map["BC1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                        map["AR3"] = (wp, np, row, esDto, sample) => row.standards!;
                        map["AX4"] = (wp, np, row, esDto, sample) => wp.WashingProcedure!;
                        map["BX4"] = (wp, np, row, esDto, sample) => wp.Temperature!;
                        map["BF5"] = (wp, np, row, esDto, sample) => wp.Ballast!;
                        map["AR6"] = (wp, np, row, esDto, sample) => wp.Detergent!;
                        map["BM6"] = (wp, np, row, esDto, sample) => wp.DryProcedure!;
                        map["BV6"] = (wp, np, row, esDto, sample) => "/ Iron";
                        map["AR7"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                    }
                    else if (GetDescValue(sample, "State", esDto)!.Contains("Garment"))
                    {
                        map["P1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                        map["A3"] = (wp, np, row, esDto, sample) => row.standards!;
                        map["I4"] = (wp, np, row, esDto, sample) => wp.WashingProcedure!;
                        map["AJ4"] = (wp, np, row, esDto, sample) => wp.Temperature!;
                        map["S5"] = (wp, np, row, esDto, sample) => wp.Ballast!;
                        map["A6"] = (wp, np, row, esDto, sample) => wp.Detergent!;
                        map["Y6"] = (wp, np, row, esDto, sample) => wp.DryProcedure!;
                        map["AH6"] = (wp, np, row, esDto, sample) => "/ Iron";
                        map["A7"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                    }
                    else if (GetDescValue(sample, "State", esDto)!.Contains("Cap")
                    || GetDescValue(sample, "State", esDto)!.Contains("Socks")
                    || GetDescValue(sample, "State", esDto)!.Contains("Gloves"))
                    {
                        map["N1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                        map["A3"] = (wp, np, row, esDto, sample) => row.standards!;
                        map["G4"] = (wp, np, row, esDto, sample) => wp.WashingProcedure!;
                        map["AL4"] = (wp, np, row, esDto, sample) => wp.Temperature!;
                        map["R5"] = (wp, np, row, esDto, sample) => wp.Ballast!;
                        map["A6"] = (wp, np, row, esDto, sample) => wp.Detergent!;
                        map["Y6"] = (wp, np, row, esDto, sample) => wp.DryProcedure!;
                        map["AH6"] = (wp, np, row, esDto, sample) => "/ Iron";
                        map["A7"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                    }
                    return map;
                },
                ["Colour Fastness to Chlorine Bleach"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["D1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A29"] = (wp, np, row, esDto, sample) => row.standards!,
                    //["L30"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("N/A") ? "N/A" : "-",
                },
                ["Colour Fastness to Non Chlorine Bleach"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["D1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A29"] = (wp, np, row, esDto, sample) => row.standards!,
                    //["L30"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("N/A") ? "N/A" : "-",
                },
                ["Colour Fastness to Dry Cleaning"] = (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["BC1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                    if (wp.DryCleanProcedure!.Contains("Petroleum"))
                    {
                        map["AR12"] = (wp, np, row, esDto, sample) => "ref" + " " + row.standards!;
                        map["BJ12"] = (wp, np, row, esDto, sample) => "With hydrocarbon solvent";
                    }
                    else map["AR12"] = (wp, np, row, esDto, sample) => row.standards!;
                    return map;
                },
                ["Colour Fastness to Hot Pressing"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["H1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A12"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["G13"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.IronMethod) ? "/" : wp.Temperature!,
                    ["R13"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.IronMethod) ? "N/A" : "-",
                },
                ["Colour Fastness to Light"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["D1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A28"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Colour Fastness to Perspiration"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["D1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Colour Fastness to PVC Migration"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["D1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Colour Fastness to Rubbing"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["D1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A20"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Colour Fastness to Saliva"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["D1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["G3"] = (wp, np, row, esDto, sample) => "√"
                },
                ["Colour Fastness to Saliva and Perspiration"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["D1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["G3"] = (wp, np, row, esDto, sample) => "√"
                },
                ["Colour Fastness to Sea Water"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["D1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A10"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Colour Fastness to Washing"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["D1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["B4"] = (wp, np, row, esDto, sample) => wp.Program!,
                    ["E4"] = (wp, np, row, esDto, sample) => wp.Temperature!,
                    ["L5"] = (wp, np, row, esDto, sample) => wp.SteelBallNum.ToString()!,
                },
                ["Colour Fastness to Water"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["D1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A35"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Dimensional and Bra Wire Casing Stability"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["BC1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["AR3"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Dye Transfer in Storage"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["BC1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["AR3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["AY4"] = (wp, np, row, esDto, sample) => "30",
                    ["BE4"] = (wp, np, row, esDto, sample) => "48"
                },
                ["Easycare/Non-Iron"] = (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["BC1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                    switch (row.standards)
                    {
                        case "AATCC TM124-2018te":
                            map["AR4"] = (wp, np, row, esDto, sample) => row.standards!;
                            break;
                        case "ISO7769:2009":
                            map["AR23"] = (wp, np, row, esDto, sample) => row.standards!;
                            break;
                    }
                    return map;
                },
                ["Phenolic Yellowing"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["BC1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["AR3"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Print / Motif / Flock Durability"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["BC1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["AR3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["AU48"] = (wp, np, row, esDto, sample) => wp.DryProcedure!,
                },
                ["Print Durability"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["BC1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["AR3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["AU48"] = (wp, np, row, esDto, sample) => wp.DryProcedure!,
                },
                ["Security of Attachment(Wash)"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["BC1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["AR4"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["AR54"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.SpecialCareInstruction) ? "-" : wp.SpecialCareInstruction
                },
                ["Stability to Dry Cleaning"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["BC1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["AW4"] = (wp, np, row, esDto, sample) => wp!.Sensitive == "Y" ? "Sensitive" : "Normal"
                },
                ["TS Board Fit"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["BC1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["AR19"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Appearance"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["BC1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["CM1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["BC57"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["CM57"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["BC114"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["CM114"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["BC171"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["CM171"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["AR3"] = (wp, np, row, esDto, sample) => "BS EN ISO 6330 & PM01"!,
                    ["CB3"] = (w, np, row, esDto, sample) => "BS EN ISO 6330 & PM01",
                    ["AR59"] = (w, np, row, esDto, sample) => "BS EN ISO 6330 & PM01",
                    ["CB59"] = (w, np, row, esDto, sample) => "BS EN ISO 6330 & PM01",
                    ["AR116"] = (wp, np, row, esDto, sample) => "BS EN ISO 6330 & PM01",
                    ["CB116"] = (wp, np, row, esDto, sample) => "BS EN ISO 6330 & PM01",
                    ["AR173"] = (wp, np, row, esDto, sample) => "BS EN ISO 6330 & PM01",
                    ["CB173"] = (wp, np, row, esDto, sample) => "BS EN ISO 6330 & PM01",
                    ["C1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                },
                ["Colour Change and Staining"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["BC1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                },
                ["Spirality"] = (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["P1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                    if (GetDescValue(sample, "State", esDto)!.Contains("Fabric")) map["A3"] = (wp, np, row, esDto, sample) => "BS EN ISO 16322-2:2021,Method A"!;
                    else if (GetDescValue(sample, "State", esDto)!.Contains("Garment")) map["A3"] = (wp, np, row, esDto, sample) => "BS EN ISO 16322-3:2021,Procedure B"!;
                    return map;
                },
            };
        /// <summary>
        /// phy参数单元格映射
        /// </summary>
        private static readonly Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto,string, 
            Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>>> BulidPhyExtraMap = new()
        {
                ["Abrasion of Knitted Footwear Garments - Modified Martindale"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>> 
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A21"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["C25"] = (wwp, np, row, esDto, sample) => "12KPa",
                    ["I25"] = (wp, np, row, esDto, sampleo) => "8000 revs",
                },
                ["Absorbency of Textiles"] = (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                    map["A3"] = (wp, np, row, esDto, sample) => row.standards!;
                    map["A31"] = (wp, np, row, esDto, sample) => wp.Bleach + " Cycle";
                    map["S30"] = (wp, np, row, esDto, sample) => wp.Temperature!;
                    map["E30"] = (wp, np, row, esDto, sample) => wp.Program!;
                    map["R31"] = (wp, np, row, esDto, sample) => wp.DryProcedure!;
                    map["H30"] = (wp, np, row, esDto, sample) => wp.DryCleanProcedure!;
                    map["AF31"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.Iron!) == true ? "/ Iron" : wp.IronMethod!;
                    map["A32"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                    if (GetDescValue(sample, "State", esDto)!.Contains("Fabric"))
                    {
                        map["A29"] = (wp, np, row, esDto, sample) => "AATCC TM 135-2018t";
                    }
                    else if (GetDescValue(sample, "State", esDto)!.Contains("Garment"))
                    {
                        map["A29"] = (wp, np, row, esDto, sample) => "AATCC TM 150-2018t/AATCC TS006";
                    }
                    return map;
                },
                ["Accelerotor"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["N5"] = (wp, np, row, esDto, sample) => "2000",
                    ["AF5"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("3") ? "3" : "5",
                },
                ["Back Pocket Application Strength"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["J1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                },
                ["Belt Loop Application Strength"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["J1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                },
                ["Chenille Pile Loss"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["J1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                },
                ["Elastic Extension and Modulus Test"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["J1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                },
                ["EU Security of Attachment on Children's Clothing"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["A17"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Fibre Proof Properties"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Fibre Shedding"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["J1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                },
                ["Martindale Abrasion"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["C5"] = (wp, np, row, esDto, sample) => "9KPa",
                    ["A6"] = (wwp, np, row, esDto, sample) => np.ExtraParam!.Contains("@ 5000")
                    ? "{<100g/m²：10000 rubs；101~199g/m²：15000 rubs；>2000g/m²：20000 rubs}"
                    : "{<200g/m²：10000 rubs；201~270g/m²：15000 rubs；271~390g/m²：18000 rubs；>390g/m²：20000 rubs}",
                    ["AA5"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("@ 5000") ? "@ 5000 revs" : "-"
                },
                ["Martindale Pilling"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["F3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["D4"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("2000 revs") ? "2000 revs" : "500 revs",
                    ["AC3"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("N/A") ? "N/A" : "-",
                    ["G40"] = (wp, np, row, esDto, sample) => wp.WashingProcedure!,
                    ["AJ40"] = (wp, np, row, esDto, sample) => wp.Temperature!,
                    ["Q41"] = (wp, np, row, esDto, sample) => wp.Ballast!,
                    ["S42"] = (wp, np, row, esDto, sample) => wp.DryProcedure!,
                    ["AB42"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.Iron) ? "/ Iron" : wp.IronMethod!
                },
                ["Mass per Unit Area"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["J1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["S3"] = (wwp, np, row, esDto, sample) =>row.parameters!.Where(s=>s.sample==sample).ToArray()[0].normalParam!.ToString()!.Contains("Single unit weight") ? "刻一个":"-" ,
                },
                ["Nap Stability"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["J1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                },
                ["Peel Bond"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Pile Retention"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["J1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                },
                ["Quick Dry"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["J1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Residual Elongation"] = (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                    map["A3"] = (wp, np, row, esDto, sample) => row.standards!;
                    map["AE9"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("N/A") ? "N/A" : "-";
                    if (GetDescValue(sample, "Structure", esDto)!.Contains("Woven"))
                    {
                        map["A5"] = (wp, np, row, esDto, sample) => GetDescValue(sample, "Test Method(Only for Extension)", esDto)!.Contains("Loop") ?
                        "Woven/Non-woven Fabric: method B---Loop trials Perimeter =200mm Speed =100mm/min"
                        : "Woven/Non-woven Fabric: method A---Stripe trials  Guage length=200mm  Speed =200mm/min.";
                    }
                    else if (GetDescValue(sample, "Structure", esDto)!.Contains("Knit"))
                    {
                        map["A5"] = (wp, np, row, esDto, sample) => GetDescValue(sample, "Test Method(Only for Extension)", esDto)!.Contains("Loop") ?
                        "Knitted Fabric: method B---Loop trials  Perimeter =200mm Speed =500mm/min" :
                        "Knitted Fabric: method A---Stripe trials Guage length=100mm Speed =500mm/min.";
                    };
                    map["L7"] = (wp, np, row, esDto, sample) => "5";
                    map["F7"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("15") ? "15"
                    : np.ExtraParam!.Contains("20") ? "20"
                    : np.ExtraParam!.Contains("25") ? "25"
                    : np.ExtraParam!.Contains("30") ? "30" : "40";
                    return map;
                },
                ["Residual Elongation SHAPEWEAR"] = (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                    map["A3"] = (wp, np, row, esDto, sample) => row.standards!;
                    map["AE9"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("N/A") ? "N/A" : "-";
                    if (GetDescValue(sample, "Structure", esDto)!.Contains("Woven"))
                    {
                        map["A5"] = (wp, np, row, esDto, sample) => GetDescValue(sample, "Test Method(Only for Extension)", esDto)!.Contains("Loop") ?
                        "Woven/Non-woven Fabric: method B---Loop trials Perimeter =200mm Speed =100mm/min"
                        : "Woven/Non-woven Fabric: method A---Stripe trials  Guage length=200mm  Speed =200mm/min.";
                    }
                    else if (GetDescValue(sample, "Structure", esDto)!.Contains("Knit"))
                    {
                        map["A5"] = (wp, np, row, esDto, sample) => GetDescValue(sample, "Test Method(Only for Extension)", esDto)!.Contains("Loop") ?
                        "Knitted Fabric: method B---Loop trials  Perimeter =200mm Speed =500mm/min" :
                        "Knitted Fabric: method A---Stripe trials Guage length=100mm Speed =500mm/min.";
                    };
                    map["L7"] = (wp, np, row, esDto, sample) => "5";
                    map["F7"] = (wp, np, row, esDto, sample) => "36";
                    return map;
                },
                ["Security of Attachment"] = (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                    if (row.standards!.Contains("17394-2")) map["A3"] = (wp, np, row, esDto, sample) => row.standards!;
                    else if (row.standards!.Contains("17394-3")) map["A18"] = (wp, np, row, esDto, sample) => row.standards!;
                    else 
                    {
                        map["A3"] = (wp, np, row, esDto, sample) => row.standards!;
                        map["A18"] = (wp, np, row, esDto, sample) => row.standards!;
                    }
                    return map;
                },
                ["Security of Attachment Buttons"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Security of Attachment Mechanically Applied Fasteners"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A18"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Sharp Edges Restrctions"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A4"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Sharp Point Restrctions"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A4"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Small Parts Restrictions"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A4"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Shower Resistant Claims Spray Rating"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["G19"] = (wp, np, row, esDto, sample) => wp.WashingProcedure!,
                    ["AJ19"] = (wp, np, row, esDto, sample) => wp.Temperature!,
                    ["P20"] = (wp, np, row, esDto, sample) => wp.Ballast!,
                    ["S21"] = (wp, np, row, esDto, sample) => wp.DryProcedure!,
                    ["AB21"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.Iron) ? "/ Iron" : wp.IronMethod!
                },
                ["Tear Strength"] = (wp, np, row, esDto, sample) => 
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                    map["A3"] = (wp, np, row, esDto, sample) => row.standards!;
                    map["AC4"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("N/A") ? "N/A":"-";
                    if (GetDescValue(sample, "Stretch Direction for Tensile and Tear", esDto)!.Contains("Warp"))
                    {
                        map["V9"] = (wp, np, row, esDto, sample) => "N/A";
                        map["AA9"] = (wp, np, row, esDto, sample) => "经向存在弹性丝，N/A";
                    }
                    if (GetDescValue(sample, "Stretch Direction for Tensile and Tear", esDto)!.Contains("Weft"))
                    {
                        map["V9"] = (wp, np, row, esDto, sample) => "N/A";
                        map["AA9"] = (wp, np, row, esDto, sample) => "纬向存在弹性丝，N/A";
                    }
                    return map;
                },
                ["Tensile Strength"] = (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                    map["A3"] = (wp, np, row, esDto, sample) => row.standards!;
                    map["AC3"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("N/A") ? "N/A" : "-";
                    if (GetDescValue(sample, "Stretch Direction for Tensile and Tear", esDto)!.Contains("Warp"))
                    {
                        map["S6"] = (wp, np, row, esDto, sample) => "N/A";
                        map["X6"] = (wp, np, row, esDto, sample) => "经向存在弹性丝，N/A";
                    }
                    if (GetDescValue(sample, "Stretch Direction for Tensile and Tear", esDto)!.Contains("Weft"))
                    {
                        map["S8"] = (wp, np, row, esDto, sample) => "N/A";
                        map["X7"] = (wp, np, row, esDto, sample) => "纬向存在弹性丝，N/A";
                    }
                    return map;
                },
                ["Unrecovered Elongation"] = (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["M1"] = (wp, np, row, esDto, sample)  => esDto.ReportNumber!;
                    map["A3"] = (wp, np, row, esDto, sample) => row.standards!;
                    map["AE9"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("N/A") ? "N/A" : "-";
                    if (GetDescValue(sample, "Structure", esDto)!.Contains("Woven"))
                    {
                        map["A5"] = (wp, np, row, esDto, sample) => GetDescValue(sample, "Test Method(Only for Extension)", esDto)!.Contains("Loop") ?
                        "Woven/Non-woven Fabric: method B---Loop trials Perimeter =200mm Speed =100mm/min"
                        : "Woven/Non-woven Fabric: method A---Stripe trials  Guage length=200mm  Speed =200mm/min.";
                    }
                    else if (GetDescValue(sample, "Structure", esDto)!.Contains("Knit"))
                    {
                        map["A5"] = (wp, np, row, esDto, sample) => GetDescValue(sample, "Test Method(Only for Extension)", esDto)!.Contains("Loop") ?
                        "Knitted Fabric: method B---Loop trials  Perimeter =200mm Speed =500mm/min" :
                        "Knitted Fabric: method A---Stripe trials Guage length=100mm Speed =500mm/min.";
                    };
                    map["L7"] = (wp, np, row, esDto, sample) => "5";
                    map["F7"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("30") ? "30" : "40";
                    return map;
                },
                ["Waterproof Claims Hydrostatic Head"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample)  => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["I8"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("1600mm") ? "1600"
                    : np.ExtraParam!.Contains("1000mm") ? "1000"
                    : np.ExtraParam!.Contains("10000mm") ? "10000"
                    : np.ExtraParam!.Contains("8000mm") ? "8000"
                    : "/",
                    ["I15"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("1600mm") ? "1600"
                    : np.ExtraParam!.Contains("1000mm") ? "1000"
                    : np.ExtraParam!.Contains("10000mm") ? "10000"
                    : np.ExtraParam!.Contains("8000mm") ? "8000"
                    : "/",
                    ["G30"] = (wp, np, row, esDto, sample) => wp.WashingProcedure!,
                    ["AJ30"] = (wp, np, row, esDto, sample) => wp.Temperature!,
                    ["P31"] = (wp, np, row, esDto, sample) => wp.Ballast!,
                    ["S32"] = (wp, np, row, esDto, sample) => wp.DryProcedure!,
                    ["AB32"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.Iron) ? "/ Iron" : wp.IronMethod!
                    //洗前洗后都有
                },
                ["Zip Fasteners"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample)  => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Vertical Wicking of Textiles"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["J1"] = (wp, np, row, esDto, sample)  => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Wind Resistant Claims Air Permeability"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample)  => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["A25"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["F5"] = (wp, np, row, esDto, sample) => "100",
                    ["E6"] = (wp, np, row, esDto, sample) => "20",
                },
                ["Physical & Mechanical"] = (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["M1"] = (wp, np, row, esDto, sample)  => esDto.ReportNumber!;
                    if (row.standards!.Contains("ASTM F963-23")) map["A3"] = (wp, np, row, esDto, sample) => row.standards!;
                    else if (row.standards!.Contains("EN 71-1:2014+A1:2018 8.4")) map["A18"] = (wp, np, row, esDto, sample) => row.standards!;
                    return map;
                },
                ["Torque & Tension"] = (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["M1"] = (wp, np, row, esDto, sample)  => esDto.ReportNumber!;
                    return map;
                },
                ["Bursting Strength"] = (wp, np, row, esDto, sample) => 
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                    if (GetDescValue(sample, "State", esDto)!.Contains("Fabric")) map["I3"] = (wp, np, row, esDto, sample) => row.standards!;
                    else if (GetDescValue(sample, "State", esDto)!.Contains("Seam")) map["I18"] = (wp, np, row, esDto, sample) => row.standards!;
                    else if (GetDescValue(sample, "State", esDto)!.Contains("Garment"))
                    {
                        map["J3"] = (wp, np, row, esDto, sample) => row.standards!;
                        var cellOrder = new List<string> { "A5", "A6", "A7", "A8", "A9", "A10", "A11", "A12", "A13", "A14", "A15", "A16" };
                        var reasonCellOrder = new List<string>();

                        if (sample.Contains("Shell") || esDto.SeamParameter!.FirstOrDefault(s => s.Sample == sample)!.Type!.Contains("Shell")) 
                        {
                            map["Q4"] = (wp, np, row, esDto, sample) => "√";
                            if (sample.Contains("Shell")) reasonCellOrder = cellOrder.Select(c => "J" + c.Substring(1)).ToList();
                        }
                        if (sample.Contains("Lining") || esDto.SeamParameter!.FirstOrDefault(s => s.Sample == sample)!.Type!.Contains("Lining"))
                        {
                            map["AF4"] = (wp, np, row, esDto, sample) => "√";
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
                        // 2.3 捞出当前样本对应的缝位信息
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
                                    map[cell] = (wp, np, row, esDto, sample) => desc;   // 填入对应描述
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
                                map[cell] = (wp, np, row, esDto, sample) => desc;
                            }

                            // 2. 当 IsNA == false 时，把 Reason 写到同行 J 列
                            if (info.IsNA == true && !string.IsNullOrWhiteSpace(info.Reason))
                            {
                                string reasonCell = reasonCellOrder[i];
                                string reason = "N/A；" + info.Reason;        // 捕获局部变量
                                map[reasonCell] = (wp, np, row, esDto, sample) => reason;
                            }
                        }
                    }
                    return map;
                },
                ["Seam Slippage"] = (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                    if (GetDescValue(sample, "State", esDto)!.Contains("Fabric")) map["A3"] = (wp, np, row, esDto, sample) => row.standards!;
                    else if (GetDescValue(sample, "State", esDto)!.Contains("Garment"))
                    {
                        map["J3"] = (wp, np, row, esDto, sample) => row.standards!;
                        var cellOrder = new List<string> { "A5", "A6", "A7", "A8", "A9", "A10", "A11", "A12", "A13", "A14", "A15", "A16" };
                        var reasonCellOrder = new List<string>();
                        if (sample.Contains("Shell") || esDto.SeamParameter!.FirstOrDefault(s => s.Sample == sample)!.Type!.Contains("Shell"))
                        {
                            map["Q4"] = (wp, np, row, esDto, sample) => "√";
                            if (sample.Contains("Shell")) reasonCellOrder = cellOrder.Select(c => "J" + c.Substring(1)).ToList();
                        }
                        if (sample.Contains("Lining") || esDto.SeamParameter!.FirstOrDefault(s => s.Sample == sample)!.Type!.Contains("Lining"))
                        {
                            map["AF4"] = (wp, np, row, esDto, sample) => "√";
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
                        // 2.3 捞出当前样本对应的缝位信息
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
                                    map[cell] = (wp, np, row, esDto, sample) => desc;   // 填入对应描述
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
                                map[cell] = (wp, np, row, esDto, sample) => desc;
                            }

                            // 2. 当 IsNA == false 时，把 Reason 写到同行 J 列
                            if (info.IsNA == true && !string.IsNullOrWhiteSpace(info.Reason))
                            {
                                string reasonCell = reasonCellOrder[i];
                                string reason = "N/A；" + info.Reason;         // 捕获局部变量
                                map[reasonCell] = (wp, np, row, esDto, sample) => reason;
                            }
                        }
                    }
                    return map;
                },
                ["Seam Strength"] = (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                    if (GetDescValue(sample, "State", esDto)!.Contains("Fabric"))
                    {
                        map["A3"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                    }
                    else if (GetDescValue(sample, "State", esDto)!.Contains("Garment"))
                    {
                        map["J18"] = (wp, np, row, esDto, sample) => row.standards!;

                        var cellOrder = new List<string> { "A20", "A21", "A22", "A23", "A24", "A25", "A26", "A27", "A28", "A29", "A30", "A31" };

                        var reasonCellOrder = new List<string>();

                        if (sample.Contains("Shell") || esDto.SeamParameter!.FirstOrDefault(s => s.Sample == sample)!.Type!.Contains("Shell"))
                        {
                            map["Q19"] = (wp, np, row, esDto, sample) => "√";
                            if (sample.Contains("Shell")) reasonCellOrder = cellOrder.Select(c => "J" + c.Substring(1)).ToList();
                        }
                        if (sample.Contains("Lining") || esDto.SeamParameter!.FirstOrDefault(s => s.Sample == sample)!.Type!.Contains("Lining"))
                        {
                            map["AF19"] = (wp, np, row, esDto, sample) => "√";
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

                        // 2.3 捞出当前样本对应的缝位信息
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
                                    map[cell] = (wp, np, row, esDto, sample) => desc;   // 填入对应描述
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
                                map[cell] = (wp, np, row, esDto, sample) => desc;
                            }

                            // 2. 当 IsNA == false 时，把 Reason 写到同行 J 列
                            if (info.IsNA == true && !string.IsNullOrWhiteSpace(info.Reason))
                            {
                                string reasonCell = reasonCellOrder[i];
                                string reason = "N/A；" + info.Reason;         // 捕获局部变量
                                map[reasonCell] = (wp, np, row, esDto, sample) => reason;
                            }
                        }
                    }
                    return map;
                },
            };
    }
}
