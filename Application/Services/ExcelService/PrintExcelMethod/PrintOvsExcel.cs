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
    public class PrintOvsExcel:IPrintExcelStrategy
    {
        private readonly LabDbContextSec _db;
        public PrintOvsExcel(LabDbContextSec db)
        {
            _db = db;
        }
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
            if (esDto.NewSelectedRows!.FirstOrDefault(row => row.itemName == "Weight per Square Meter") == null)
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
                        itemName = "Weight per Square Meter",
                        standards = "ISO 3801:1977",
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

            //主逻辑循环
            int groupIndex = 0;// 将groupIndex移到项目循环外部，使其在所有项目中持续递增

            foreach (var row in esDto.NewSelectedRows!)
            {
                var pkg = row.types == "Wet" ? pkgWet : pkgPhy;

                var groups = BuildGroupsWithExpansion(row, esDto.ReportNumber!);

                foreach (var group in groups)
                {
                    // 组内代表点（第一个）
                    var representative = group.Points.First();
                    // 获取水洗映射
                    var afMap = group.Points.FirstOrDefault()?.AfterWashMap;

                    // 获取描述值（如有需要）
                    var descValue = GetDescValue(representative.Code, "State", esDto);

                    // 1选择模板
                    var selector = new TemplateSelector(TemplateSheetNames, TemplateSheetNamesNormal);

                    var templateName = selector.GetTemplateName(row.itemName!, descValue!);

                    templateName = SelectTemplate(row.itemName!, row.standards!, templateName);

                    // 在当前测点组获取模板Sheet
                    var template = pkg.Workbook.Worksheets[templateName];

                    // 2计算容量和Sheet数
                    var allDisplayNames = group.Points.SelectMany(p => p.Expanded.Select(e => e.DisplayName)).ToList();             //当前组的所有测点

                    var cellAddrs = GetCellAddresses(row.itemName!, row.standards!, descValue);                                                      //先去拿到单元格地址

                    var capacity = GetCapacity(row.itemName!, row.standards!, cellAddrs.Length, descValue);                          //计算容量

                    var sheetCnt = (int)Math.Ceiling(allDisplayNames.Count / (double)capacity);

                    var sheets = new List<ExcelWorksheet> { template };
                    for (int i = 1; i < sheetCnt; i++)
                    {
                        string name = $"{templateName}_G{groupIndex}_{i + 1}";  // 加组索引，避免多组命名冲突
                        sheets.Add(pkg.Workbook.Worksheets.Any(ws => ws.Name == name)
                            ? pkg.Workbook.Worksheets[name]
                            : pkg.Workbook.Worksheets.Copy(templateName, name));
                    }

                    // 3切片
                    var slices = BuildSlices(allDisplayNames, capacity);

                    // 每张Sheet填一个切片
                    for (int idx = 0; idx < slices.Count; idx++)
                    {
                        FillSlice(sheets[idx], slices[idx], group, row, esDto.ReportNumber!, esDto, afMap);
                    }

                    groupIndex++;//用于获取测点组索引，方便sheet命名
                }
                groupIndex = 0;//重置组索引，以便下一个项目重新开始
            }


            // 在 PrintJsonData 方法最后，Save 之前添加
            var colorFastnessItems = new[] { "Colour Fastness to Rubbing", "Colour Fastness to Water", "Colour Fastness to Perspiration" };

            // 第1步：筛选出所有色牢度项目（只执行一次）
            var colorFastnessRows = esDto.NewSelectedRows!
                .Where(r => colorFastnessItems.Contains(r.itemName))
                .ToList();

            if (!colorFastnessRows.Any()) return;


            // 第2步：查找目标 Sheets（只执行一次）
            var targetSheets = pkgWet.Workbook.Worksheets
                .Where(ws => ws.Name.Contains("CFtoPWR"))
                .ToList();

            // 第3步：遍历每个 Sheet，只复制一次
            foreach (var sheet in targetSheets)
            {
                // 复制工作表（每个 sheet 只复制一次）
                var gb1 = pkgWet.Workbook.Worksheets.Copy(sheet.Name, $"{sheet.Name}_GB1");
                var gb2 = pkgWet.Workbook.Worksheets.Copy(sheet.Name, $"{sheet.Name}_GB2");
                var isoName = $"{sheet.Name}_ISO";
                pkgWet.Workbook.Worksheets[sheet.Name].Name = isoName;

                var isoSheet = pkgWet.Workbook.Worksheets[isoName];

                // 第4步：遍历每个色牢度项目，修改对应单元格
                foreach (var row in colorFastnessRows)
                {
                    // 根据具体项目确定 GB 标准
                    string gbStandard = row.itemName switch
                    {
                        "Colour Fastness to Rubbing" => "GB/T 3920-2008",
                        "Colour Fastness to Perspiration" => "GB/T 3922-2013",
                        "Colour Fastness to Water" => "GB/T 5713-2013",
                        _ => throw new ArgumentException($"未知的项目: {row.itemName}")
                    };

                    ModifyStandardInSheet(isoSheet, row, "ISO", gbStandard);
                    ModifyStandardInSheet(gb1, row, "GB", gbStandard);
                    ModifyStandardInSheet(gb2, row, "GB", gbStandard);
                }
            }

            pkgWet.Save();
            pkgPhy.Save();
        }

        /// <summary>
        /// 用于2C报告色牢度打印
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="row"></param>
        /// <param name="standardType"></param>
        /// <param name="gbStandard"></param>
        /// <exception cref="ArgumentException"></exception>
        private void ModifyStandardInSheet(ExcelWorksheet sheet, NewSelectedRows row, string standardType, string gbStandard)
        {
            // 根据项目确定单元格地址
            var standardCell = row.itemName switch
            {
                "Colour Fastness to Rubbing" => "A40",
                "Colour Fastness to Perspiration" => "A3",
                "Colour Fastness to Water" => "A27",
                _ => throw new ArgumentException($"未知的项目: {row.itemName}")
            };

            // 确定要填入的标准值
            var newStandard = standardType == "ISO" ? row.standards! : gbStandard;

            // 直接修改单元格
            sheet.Cells[standardCell].Value = newStandard;
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
            int[]? afMap, string sample,
            SampleGroup group)
        {
            // 1. 填测点名称（在 FillSlice 已做）

            // 2. 填 AfterWash（如有）
            if (AfterWashCellMapper.ContainsKey(row.itemName!))
            {
                var descValue = GetDescValue(group.Points[0].Code, "State", esDto);

                var afterWashAddrs = AfterWashCellMapper[row.itemName!](row.itemName!, row.standards!, descValue!);

                WriteAfterWash(sheet, afMap, afterWashAddrs, row.itemName!, row.standards!, descValue!);
            }

            // 3. 填参数（ExtraMap）
            var extraMap = bag.Type == "Wet"
           ? BulidWetExtraMap!.GetValueOrDefault(row.itemName, (wp, np, row, esDto, sample)
                => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>())(bag.WetParam!, bag.NormalParam!, row, esDto, sample)
           : BulidPhyExtraMap!.GetValueOrDefault(row.itemName, (wp, np, row, esDto, sample)
                => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>())(bag.WetParam!, bag.NormalParam!, row, esDto, sample);

            foreach (var kv in extraMap)
            {
                sheet.Cells[kv.Key].Value = kv.Value(bag.WetParam!, bag.NormalParam!, row, esDto, sample!);
            }
        }

        /// <summary>
        /// 测点参数包，只和原测点有关
        /// </summary>
        /// <param name="row"></param>
        /// <param name="reportNo"></param>
        /// <param name="buyer"></param>
        /// <returns></returns>
        private Dictionary<string, ParameterBag> LoadParamBagsByItemName(NewSelectedRows row, string reportNo, string buyer)
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
                    .Where(p => p.Name != "ContactSample" && p.Name != "ParamId")
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
            var bags = LoadParamBagsByItemName(row, reportNo, "Ovs");
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
            var needExpand = itemName is "Appearance after Washing/Dry-Cleaning" or "Dimensional Stability to Washing"
              or "Movement after Washing";

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
                    _ when standard.Contains("EN 71-1:2024+A1:2018") => "Attachment Strength",
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

            if (itemName is "Appearance after Washing/Dry-Cleaning") return 1;

            if (itemName == "Dimensional Stability to Washing" && ! descValue.Contains("Fabric")) return 1;

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
            if ((itemName == "Dimensional Stability to Washing") && ! descValue.Contains("Fabric")) offset = 0;

            if (itemName == "Appearance after Washing/Dry-Cleaning")
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
                "Appearance after Washing/Dry-Cleaning"
                    => Enumerable.Repeat(afmap[0], AfterWashCellAddrs.Length),

                "Dimensional Stability to Washing" when ! sampleDescription.Contains("Fabric")
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
            ["Dimensional Stability to Steaming"] = 3,
            ["Dimensional Stability to Washing"] = 4,
            ["Dimensional Stability to Dry-Cleaning"] = 4,
            ["Colour Fastness to Non Chlorine Bleach"] = 6,
            //["Water Permeability/Hydrostatic Head"] = 2
        };
        /// <summary>
        /// sheet名称
        /// </summary>
        private static readonly Dictionary<string, string> TemplateSheetNamesNormal = new()
        {
            ["Colour Fastness to Washing"] = "CFtoWSL&PVC",
            ["Colour Fastness to Rubbing"] = "CFtoPWR",
            ["Colour Fastness to Light"] = "CFtoWSL&PVC",
            ["Colour Fastness to Migration on PVC"] = "CFtoWSL&PVC",
            ["Colour Fastness to Perspiration"] = "CFtoPWR",
            ["Colour Fastness to Water"] = "CFtoPWR",
            ["Colour Fastness to Sea Water"] = "CFtoWSL&PVC",
            ["Colour Fastness to Saliva"] = "CFtoSaliva&Sweat",
            ["Colour Fastness to Sweat"] = "CFtoSaliva&Sweat",
            ["Colour Fastness to Phenolic Yellowing"] = "CFtoYellow&Cl&Bleach",
            ["Colour Fastness to Chlorine Bleach"] = "CFtoYellow&Cl&Bleach",
            ["Colour Fastness to Non Chlorine Bleach"] = "CFtoYellow&Cl&Bleach",
            ["Colour Fastness to Chlorinated Water"] = "CFtoYellow&Cl&Bleach",
            ["Calculation of Color Differences"] = "CMC&Sublimation",
            ["Colour Fastness to Sublimation in Storage"] = "CMC&Sublimation",
            ["Movement after Washing"] = "Spirality",
            ["Dimensional Stability to Steaming"] = "StabilitytoSteam",
            ["Dimensional Stability to Dry-Cleaning"] = "StabilitytoDryClean",
            ["Appearance after Washing/Dry-Cleaning"] = "AppearanceAfterWashing",

            ["Weight per Square Meter"] = "Weight",
            ["Fabric Width"] = "Width",
            ["Abrasion Resistance"] = "Abrasion Resistance",
            ["Pilling Resistance"] = "Pilling Resistance",
            ["Drying Rate"] = "DryingRate",
            ["Vertical Wicking"] = "Wicking",
            ["Stretch & Recovery"] = "Elastic",
            ["Tensile Strength"] = "Tensile Strength",
            ["Tear Strength"] = "Tearing Strength",
            ["Slide Fastness(Zipper)"] = "Zipper Strength",
            ["Pull Test"] = "Attachment Strength",
            ["Absorbency"] = "Absorbency",
            ["Moisture Management"] = "Moisture Management",
            ["Air Permeability"] = "Air Permeability",
            ["Electrostatic Properties"] = "Electrostatic Properties",
            ["Water Permeability/Hydrostatic Head"] = "Hydrostatic",
            ["Spray Test"] = "Water Repellency",
            ["Density"] = "Density",
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
                {new[] { "Garment" },"Seam Slippage&Strength-G"},
            },
            [("Bursting Strength")] = new Dictionary<string[], string>
            {
                {new[] { "Fabric" }, "Bursting Strength" },
                {new[] { "Garment" },"Bursting Strength-G"},
            },
            [("Dimensional Stability to Washing")] = new Dictionary<string[], string>
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
            ["Colour Fastness to Washing"] = (n, m, l) => ExcelOVSMapper.MapWLPS(n),
            ["Colour Fastness to Rubbing"] = (n, m, l) => ExcelOVSMapper.MapPWR(n),
            ["Colour Fastness to Light"] = (n, m, l) => ExcelOVSMapper.MapWLPS(n),
            ["Colour Fastness to Migration on PVC"] = (n, m, l) => ExcelOVSMapper.MapWLPS(n),
            ["Colour Fastness to Perspiration"] = (n, m, l) => ExcelOVSMapper.MapPWR(n),
            ["Colour Fastness to Water"] = (n, m, l) => ExcelOVSMapper.MapPWR(n),
            ["Colour Fastness to Sea Water"] = (n, m, l) => ExcelOVSMapper.MapWLPS(n),
            ["Colour Fastness to Saliva"] = (n, m, l) => ExcelOVSMapper.MapCFtoSalivaSweat(),
            ["Colour Fastness to Sweat"] = (n, m, l) => ExcelOVSMapper.MapCFtoSalivaSweat(),
            ["Colour Fastness to Phenolic Yellowing"] = (n, m, l) => ExcelOVSMapper.MapYCB(n),
            ["Colour Fastness to Chlorine Bleach"] = (n, m, l) => ExcelOVSMapper.MapYCB(n),
            ["Colour Fastness to Non Chlorine Bleach"] = (n, m, l) => ExcelOVSMapper.MapYCB(n),
            ["Colour Fastness to Chlorinated Water"] = (n, m, l) => ExcelOVSMapper.MapYCB(n),
            ["Calculation of Color Differences"] = (n, m, l) => ExcelOVSMapper.MapCS(n),
            ["Colour Fastness to Sublimation in Storage"] = (n, m, l) => ExcelOVSMapper.MapCS(n),
            ["Movement after Washing"] = (n, m, l) => ExcelOVSMapper.MapSpirality(l),
            ["Dimensional Stability to Steaming"] = (n, m, l) => ExcelOVSMapper.MapSteam(),
            ["Dimensional Stability to Dry-Cleaning"] = (n, m, l) => ExcelOVSMapper.MapStabilityToDryClean(),
            ["Appearance after Washing/Dry-Cleaning"] = (n, m, l) => ExcelOVSMapper.MapAppearance(),
            ["Dimensional Stability to Washing"] = (n, m, l) => ExcelOVSMapper.MapStability(l),

            ["Weight per Square Meter"] = (n, m, l) => ExcelOVSMapper.MapWeight(),
            ["Fabric Width"] = (n, m, l) => ExcelOVSMapper.MapWidth(),
            ["Abrasion Resistance"] = (n, m, l) => ExcelOVSMapper.MapAbrasion(),
            ["Pilling Resistance"] = (n, m, l) => ExcelOVSMapper.MapPilling(m),
            ["Drying Rate"] = (n, m, l) => ExcelOVSMapper.MapDryRate(),
            ["Vertical Wicking"] = (n, m, l) => ExcelOVSMapper.MapWicking(),
            ["Stretch & Recovery"] = (n, m, l) => ExcelOVSMapper.MapElastic(),
            ["Tensile Strength"] = (n, m, l) => ExcelOVSMapper.MapTensile(),
            ["Tear Strength"] = (n, m, l) => ExcelOVSMapper.MapTear(),
            ["Slide Fastness(Zipper)"] = (n, m, l) => ExcelOVSMapper.MapZipper(),
            ["Pull Test"] = (n, m, l) => ExcelOVSMapper.MapAttachmentStrength(),
            ["Absorbency"] = (n, m, l) => ExcelOVSMapper.MapAbsorbency(),
            ["Moisture Management"] = (n, m, l) => ExcelOVSMapper.MapMoisture(),
            ["Air Permeability"] = (n, m, l) => ExcelOVSMapper.MapAir(),
            ["Electrostatic Properties"] = (n, m, l) => ExcelOVSMapper.MapElectrostatic(),
            ["Water Permeability/Hydrostatic Head"] = (n, m, l) => ExcelOVSMapper.MapHydroatatic(),
            ["Spray Test"] = (n, m, l) => ExcelOVSMapper.MapRepellency(),
            ["Density"] = (n, m, l) => ExcelOVSMapper.MapDensity(),
            ["Seam Strength"] = (n, m, l) => ExcelOVSMapper.MapSlippageStrength(n,l),
            ["Bursting Strength"] = (n, m, l) => ExcelOVSMapper.MapBursting(l),
            ["Seam Slippage"] = (n, m, l) => ExcelOVSMapper.MapSlippageStrength(n, l),
        };
        /// <summary>
        /// 洗涤遍数
        /// </summary>
        private static readonly Dictionary<string, Func<string, string, string, string[]>> AfterWashCellMapper = new()
        {
            ["Movement after Washing"] = (n, m, l) => ExcelOVSMapper.SpiralityAf(),
            ["Dimensional Stability to Washing"] = (n, m, l) => ExcelOVSMapper.StabilityAf(l),
            ["Dimensional Stability to Dry-Cleaning"] = (n, m, l) => ExcelOVSMapper.DStoDCAf(),
            ["Security of Attachment(Wash)"] = (n, m, l) => ExcelOVSMapper.AttachmentAf(),
            ["Appearance after Washing/Dry-Cleaning"] = (n, m, l) => ExcelOVSMapper.AppearanceAf(),
        };
        /// <summary>
        /// wet参数单元格映射
        /// </summary>
        private static readonly Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string,
            Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>>> BulidWetExtraMap = new()
            {
                ["Colour Fastness to Washing"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["D1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["B4"] = (wp, np, row, esDto, sample) => wp.Program!,
                    ["E4"] = (wp, np, row, esDto, sample) => wp.Temperature!,
                    ["L5"] = (wp, np, row, esDto, sample) => wp.SteelBallNum.ToString()!,
                },
                ["Colour Fastness to Rubbing"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["D1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A40"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Colour Fastness to Light"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["D1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A19"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["B22"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("L-5")?"L-5"
                    : np.ExtraParam!.Contains("L-4")?"L-4" 
                    :"L-3"!,
                },
                ["Colour Fastness to PVC Migration"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["D1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A27"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Colour Fastness to Phenolic Yellowing"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["H1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Colour Fastness to Chlorine Bleach"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["H1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A21"] = (wp, np, row, esDto, sample) => row.standards!,
                    //["N22"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("N/A") ? "N/A" : "-",
                },
                ["Colour Fastness to Non Chlorine Bleach"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["H1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A21"] = (wp, np, row, esDto, sample) => row.standards!,
                    //["N22"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("N/A") ? "N/A" : "-",
                },
                ["Colour Fastness to Chlorinated Water"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["H1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A12"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["E13"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("50") ? "50mg/L" : "20mg/L",
                },
                ["Colour Fastness to Perspiration"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["D1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Colour Fastness to Sea Water"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["D1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A34"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Colour Fastness to Water"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["D1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A27"] = (wp, np, row, esDto, sample) => row.standards!,
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
                ["Calculation of Color Differences"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["H1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Dimensional Stability to Dry-Cleaning"] = (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["BC1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                    if (wp.DryCleanProcedure!.Contains("Petroleum"))
                    {
                        map["AR3"] = (wp, np, row, esDto, sample) => row.standards!;
                        map["AW4"] = (wp, np, row, esDto, sample) => wp!.Sensitive == "Y" ? "Sensitive" : "Normal";
                    }
                    else map["AR3"] = (wp, np, row, esDto, sample) => row.standards!;
                    return map;
                },
                ["Dimensional Stability to Steaming"]= (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["BC1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                    map["AR3"] = (wp, np, row, esDto, sample) => row.standards!;
                    return map;
                },
                ["Dimensional Stability to Washing"] = (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    if (GetDescValue(sample, "State", esDto)!.Contains("Fabric"))
                    {
                        map["BC1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                        map["AR3"] = (wp, np, row, esDto, sample) => row.standards!;
                        map["AX4"] = (wp, np, row, esDto, sample) => wp.WashingProcedure!;
                        map["BX4"] = (wp, np, row, esDto, sample) => wp.Temperature!;
                        map["BF5"] = (wp, np, row, esDto, sample) => wp.Ballast!;
                        map["BI6"] = (wp, np, row, esDto, sample) => wp.DryProcedure!;
                        map["BR6"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.Iron) == true ? "/ Iron" : wp.IronMethod!; 
                        map["AR7"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                    }
                    else if (GetDescValue(sample, "State", esDto)!.Contains("Garment"))
                    {
                        map["P1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                        map["A3"] = (wp, np, row, esDto, sample) => row.standards!;
                        map["I4"] = (wp, np, row, esDto, sample) => wp.WashingProcedure!;
                        map["AJ4"] = (wp, np, row, esDto, sample) => wp.Temperature!;
                        map["S5"] = (wp, np, row, esDto, sample) => wp.Ballast!;
                        map["V6"] = (wp, np, row, esDto, sample) => wp.DryProcedure!;
                        map["AE6"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.Iron) == true ? "/ Iron" : wp.IronMethod!;
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
                        map["T6"] = (wp, np, row, esDto, sample) => wp.DryProcedure!;
                        map["AD6"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.Iron) == true ? "/ Iron" : wp.IronMethod!;
                        map["A7"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                    }
                    return map;
                },
                ["Security of Attachment(Wash)"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["BC1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["AR4"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["AX52"] = (wp, np, row, esDto, sample) => wp.WashingProcedure!,
                    ["BW52"] = (wp, np, row, esDto, sample) => wp.Temperature!,
                    ["BF53"] = (wp, np, row, esDto, sample) => wp.Ballast!,
                    ["BH54"] = (wp, np, row, esDto, sample) => wp.DryProcedure!,
                    ["BQ54"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.Iron!) == true ? "/ Iron" : wp.IronMethod!,
                    ["AR55"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!
                },
                ["Appearance after Washing/Dry-Cleaning"] = (wp, np, row, esDto, sample) => 
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["BC1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                    map["AR4"] = (wp, np, row, esDto, sample) => row.standards!;
                    map["BI13"] = (wp, np, row, esDto, sample) => wp.IronMethod!;

                    if (esDto.NewSelectedRows!.Any(row => row.itemName!.Contains("Dimensional Stability to Dry-Cleaning")))
                    {
                        map["AR2"] = (wp, np, row, esDto, sample) => "Appearance After Dry-cleaning 干洗后外观";
                        map["BG13"] = (wp, np, row, esDto, sample) => "dry-clean";
                        map["BJ42"] = (wp, np, row, esDto, sample) => wp!.Sensitive == "Y" ? "Sensitive" : "Normal";
                    }
                    else 
                    {
                        map["AR2"] = (wp, np, row, esDto, sample) => "Appearance After Washing 水洗后外观";
                        map["BG13"] = (wp, np, row, esDto, sample) => "wash";
                        map["AZ37"] = (wp, np, row, esDto, sample) => "ISO 5077:2007/ISO 3759:2011/ISO 6330:2021";
                        map["AX38"] = (wp, np, row, esDto, sample) => wp.WashingProcedure!;
                        map["BX38"] = (wp, np, row, esDto, sample) => wp.Temperature!;
                        map["BG39"] = (wp, np, row, esDto, sample) => wp.Ballast!;
                        map["BI40"] = (wp, np, row, esDto, sample) => wp.DryProcedure!;
                        map["BR40"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.Iron!) == true ? "/ Iron" : wp.IronMethod!;
                        map["AR43"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                    }
                    return map;
                },
                ["Movement after Washing"] = (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["P1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                    if (GetDescValue(sample, "State", esDto)!.Contains("Garment")) map["A3"] = (wp, np, row, esDto, sample) => "AATCC TM 179-2023, Method 2, Option 3";
                    else if (GetDescValue(sample, "State", esDto)!.Contains("Fabric")) map["A3"] = (wp, np, row, esDto, sample) => "AATCC TM 179-2023, Method 1, Option 1";
                    if (wp.WashingProcedure!.Contains("H"))
                    {
                        map["T36"] = (wp, np, row, esDto, sample) => wp.Temperature!;
                        map["Y36"] = (wp, np, row, esDto, sample) => wp.DryProcedure!;
                    }
                    else
                    {
                        map["A32"] = (wp, np, row, esDto, sample) => (GetDescValue(sample, "State", esDto)!.Contains("Fabric"))? "AATCC TM135-2018t ": "AATCC TM150-2018t";
                        map["R32"] = (wp, np, row, esDto, sample) => wp.Program!;
                        map["U32"] = (wp, np, row, esDto, sample) => wp.Bleach!;
                        map["AE32"] = (wp, np, row, esDto, sample) => wp.Temperature!;
                        map["A33"] = (wp, np, row, esDto, sample) => wp.Ballast!;
                        map["M33"] = (wp, np, row, esDto, sample) => wp.DryProcedure!;
                        map["V33"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.Iron!) == true ? "/ Iron" : wp.IronMethod!;
                    }
                    return map;
                },
            };
        /// <summary>
        /// phy参数单元格映射
        /// </summary>
        private static readonly Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string,
            Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>>> BulidPhyExtraMap = new()
            {
                ["Weight per Square Meter"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["J1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["S3"] = (wwp, np, row, esDto, sample) => row.parameters!.Where(s => s.sample == sample).ToArray()[0].normalParam!.ToString()!.Contains("Single unit weight") ? "刻一个" : "-",
                },
                ["Fabric Width"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Drying Rate"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["J1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Absorbency"] = (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                    map["A3"] = (wp, np, row, esDto, sample) => row.standards!;
                    if (np.ExtraParam!.Contains("1 Cycle"))
                    {
                        map["N7"] = (wp, np, row, esDto, sample) => "1";
                        map["A22"] = (wp, np, row, esDto, sample) => wp.Bleach + " Cycle";
                        map["S21"] = (wp, np, row, esDto, sample) => wp.Temperature!;
                        map["E21"] = (wp, np, row, esDto, sample) => wp.Program!;
                        map["R22"] = (wp, np, row, esDto, sample) => wp.DryProcedure!;
                        map["H21"] = (wp, np, row, esDto, sample) => wp.DryCleanProcedure!;
                        map["AF22"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.Iron!) == true ? "/ Iron" : wp.IronMethod!;
                        map["A23"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!;
                        if (GetDescValue(sample, "State", esDto)!.Contains("Fabric"))
                        {
                            map["A20"] = (wp, np, row, esDto, sample) => "AATCC TM 135-2018t";
                        }
                        else if (GetDescValue(sample, "State", esDto)!.Contains("Garment"))
                        {
                            map["A20"] = (wp, np, row, esDto, sample) => "AATCC TM 150-2018t/AATCC TS006";
                        }
                    }
                    else
                    {
                        map["A7"] = (wp, np, row, esDto, sample) => "√";
                    }
                    return map;
                },
                ["Fibre Proof Properties"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Abrasion Resistance"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["AC3"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("N/A") ? "N/A" : "-",
                    ["C5"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("12KPa") ? "12KPa" : np.ExtraParam.Contains("3KPa") ? "3KPa" : "9KPa",
                    ["I5"] = (wwp, np, row, esDto, sample) => np.ExtraParam!.Contains("at 20000 revs") ? "20000 revs"
                    : np.ExtraParam!.Contains("at 30000 revs") ? "30000 revs" :
                    np.ExtraParam!.Contains("at 10000 revs") ? "10000 revs"
                    : "15000 revs",
                    ["AA5"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("@ 3000") ? "@ 3000 revs"
                    : np.ExtraParam!.Contains("@ 20000") ? "@ 20000 revs"
                    : "- ",
                    ["A6"] = (wwp, np, row, esDto, sample) => np.ExtraParam!.Contains("at 20000 revs") ? "Evaluation at 20000 revs; CC ≥ 3-4 @ 3000 revs"
                    : np.ExtraParam!.Contains("at 30000 revs") ? "Evaluation at 30000 revs; CC ≥ 3-4 @ 3000 revs" :
                    np.ExtraParam!.Contains("at 10000 revs") ? "Evaluation at 10000 revs; CC ≥ 3-4 @ 3000 revs"
                    : "Evaluation at 15000 revs; CC ≥ 3-4 @ 3000 revs",
                },
                ["Pilling Resistance"] = (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                    if (row.standards!.Contains("ISO 12945-1:2020"))
                    {
                        if (GetDescValue(sample, "Washing Cycle(Only for Pilling)", esDto)!.Contains("After 1 Cycle"))
                        {
                            map["T5"] = (wp, np, row, esDto, sample) => "1";
                            map["V5"] = (wp, np, row, esDto, sample) => "Wash";
                            map["G31"] = (wp, np, row, esDto, sample) => wp.WashingProcedure!;
                            map["AJ31"] = (wp, np, row, esDto, sample) => wp.Temperature!;
                            map["Q32"] = (wp, np, row, esDto, sample) => wp.Ballast!;
                            map["L33"] = (wp, np, row, esDto, sample) => wp.DryProcedure!;
                            map["U33"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.Iron) ? "/ Iron" : wp.IronMethod!;
                            map["A34"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) ? "-" : wp.SpecialCareInstruction!;
                        }
                        else
                        {
                            map["T5"] = (wp, np, row, esDto, sample) => "1";
                            map["V5"] = (wp, np, row, esDto, sample) => "Dry-clean";
                            map["L36"] = (wp, np, row, esDto, sample) => wp.Sensitive!.Contains("Sensitive") ? "Sensitive" : "Normal";
                        }
                        map["G3"] = (wp, np, row, esDto, sample) => row.standards!;
                        map["AC3"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("N/A") ? "N/A" : "-";
                        map["D4"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("7200") ? "7200 revs" : "10800 revs";
                    }
                    else
                    {
                        if (GetDescValue(sample, "Washing Cycle(Only for Pilling)", esDto)!.Contains("After 1 Cycle"))
                        {
                            map["T15"] = (wp, np, row, esDto, sample) => "1";
                            map["T22"] = (wp, np, row, esDto, sample) => "1";
                            map["V15"] = (wp, np, row, esDto, sample) => "Wash";
                            map["V22"] = (wp, np, row, esDto, sample) => "Wash";
                            map["G31"] = (wp, np, row, esDto, sample) => wp.WashingProcedure!;
                            map["AJ31"] = (wp, np, row, esDto, sample) => wp.Temperature!;
                            map["Q32"] = (wp, np, row, esDto, sample) => wp.Ballast!;
                            map["L33"] = (wp, np, row, esDto, sample) => wp.DryProcedure!;
                            map["U33"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.Iron) ? "/ Iron" : wp.IronMethod!;
                            map["A34"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) ? "-" : wp.SpecialCareInstruction!;
                        }
                        else
                        {
                            map["T15"] = (wp, np, row, esDto, sample) => "1";
                            map["T22"] = (wp, np, row, esDto, sample) => "1";
                            map["V15"] = (wp, np, row, esDto, sample) => "Dry-clean";
                            map["V22"] = (wp, np, row, esDto, sample) => "Dry-clean";
                            map["L36"] = (wp, np, row, esDto, sample) => wp.Sensitive!.Contains("Sensitive") ? "Sensitive" : "Normal";
                        }
                        map["F13"] = (wp, np, row, esDto, sample) => row.standards!;
                        map["AC3"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("N/A") ? "N/A" : "-";
                        map["D4"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("2000") ? "2000 revs" : "-";
                    }
                    return map;
                },
                ["Stretch & Recovery"] = (wp, np, row, esDto, sample) =>
                {
                    var map = new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>();
                    map["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!;
                    map["A3"] = (wp, np, row, esDto, sample) => row.standards!;
                    map["AC7"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("N/A") ? "N/A" : "-";
                    if (GetDescValue(sample, "Structure", esDto)!.Contains("Woven"))
                    {
                        map["A5"] = (wp, np, row, esDto, sample) => "Woven/Non-woven Fabric: method A---Stripe trials  Guage length=200mm  Speed =200mm/min.";
                    }
                    map["L7"] = (wp, np, row, esDto, sample) => "5";
                    map["F7"] = (wp, np, row, esDto, sample) => "30";
                    return map;
                },
                ["Pull Test"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["A21"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Spray Test"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["N5"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("1 Cycle") ? "1" : np.ExtraParam!.Contains("5 Cycle") ? "5" : "-",
                    ["A5"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("Original Sample") ? "1" : "",
                    ["G20"] = (wp, np, row, esDto, sample) => wp.WashingProcedure!,
                    ["AJ20"] = (wp, np, row, esDto, sample) => wp.Temperature!,
                    ["Q21"] = (wp, np, row, esDto, sample) => wp.Ballast!,
                    ["L22"] = (wp, np, row, esDto, sample) => wp.DryProcedure!,
                    ["U22"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.Iron) ? "/ Iron" : wp.IronMethod!,
                    ["A23"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.SpecialCareInstruction) ? "-" : wp.SpecialCareInstruction!,
                },
                ["Tear Strength"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Tensile Strength"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,

                },
                ["Moisture Management"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["AQ1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,

                },
                ["Water Permeability/Hydrostatic Head"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["I8"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("1800mmH2O") ? "1800"
                    : np.ExtraParam!.Contains("20000mmH2O") ? "20000"
                    : np.ExtraParam!.Contains("3000mmH2O") ? "3000"
                    : np.ExtraParam!.Contains("5000mmH2O") ? "5000"
                    : "/",
                    ["I15"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("1800mmH2O") ? "1800"
                    : np.ExtraParam!.Contains("20000mmH2O") ? "20000"
                    : np.ExtraParam!.Contains("3000mmH2O") ? "3000"
                    : np.ExtraParam!.Contains("5000mmH2O") ? "5000"
                    : "/",
                    ["D15"] = (wp, np, row, esDto, sample) => np.ExtraParam!.Contains("1 Cycle") ? "1" : np.ExtraParam!.Contains("3 Cycle") ? "3" : np.ExtraParam!.Contains("5 Cycle") ? "5" : "-",
                    ["G30"] = (wp, np, row, esDto, sample) => wp.WashingProcedure!,
                    ["AJ30"] = (wp, np, row, esDto, sample) => wp.Temperature!,
                    ["P31"] = (wp, np, row, esDto, sample) => wp.Ballast!,
                    ["S32"] = (wp, np, row, esDto, sample) => wp.DryProcedure!,
                    ["AB32"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.Iron) ? "/ Iron" : wp.IronMethod!
                    //洗前洗后都有
                },
                ["Slide Fastness(Zipper)"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Vertical Wicking"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["J1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Air Permeability"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["A25"] = (wp, np, row, esDto, sample) => row.standards!,
                    ["F5"] = (wp, np, row, esDto, sample) => "100",
                    ["E6"] = (wp, np, row, esDto, sample) => "20",
                    ["G30"] = (wp, np, row, esDto, sample) => wp.WashingProcedure!,
                    ["AJ30"] = (wp, np, row, esDto, sample) => wp.Temperature!,
                    ["Q31"] = (wp, np, row, esDto, sample) => wp.Ballast!,
                    ["L32"] = (wp, np, row, esDto, sample) => wp.DryProcedure!,
                    ["U32"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.Iron) == true ? "/ Iron" : wp.IronMethod!,
                    ["A33"] = (wp, np, row, esDto, sample) => string.IsNullOrEmpty(wp.SpecialCareInstruction!) == true ? "-" : wp.SpecialCareInstruction!,
                },
                ["Electrostatic Properties"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
                },
                ["Density"] = (wp, np, row, esDto, sample) => new Dictionary<string, Func<WetParameterIso, NormalParameter, NewSelectedRows, ExcelSubmitDto, string, string>>
                {
                    ["M1"] = (wp, np, row, esDto, sample) => esDto.ReportNumber!,
                    ["A3"] = (wp, np, row, esDto, sample) => row.standards!,
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
                        var reasonCellOrder = cellOrder.Select(c => "Y" + c.Substring(1)).ToList();

                        if (sample.ToLower().Contains("shell"))
                        {
                            if (sample.Contains("Shell")) reasonCellOrder = cellOrder.Select(c => "J" + c.Substring(1)).ToList();
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
                        var reasonCellOrder = cellOrder.Select(c => "Y" + c.Substring(1)).ToList();
                        if (sample.ToLower().Contains("shell"))
                        {
                            if (sample.Contains("Shell")) reasonCellOrder = cellOrder.Select(c => "J" + c.Substring(1)).ToList();
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

                        var reasonCellOrder = cellOrder.Select(c => "Y" + c.Substring(1)).ToList();

                        if (sample.ToLower().Contains("shell"))
                        {
                            if (sample.Contains("Shell")) reasonCellOrder = cellOrder.Select(c => "J" + c.Substring(1)).ToList();
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
