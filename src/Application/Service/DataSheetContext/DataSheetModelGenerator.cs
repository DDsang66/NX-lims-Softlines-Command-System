using Newtonsoft.Json;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Application.Interface.StandardContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Service;
using NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Application.Service.DataSheetContext
{
    /// <summary>
    /// 用于生成模板引擎所需要的数据结构
    /// </summary>
    public class DataSheetModelGenerator : IScopedDependency, IDataSheetModelGenerator
    {
        private readonly IStandardRepository _standardRepository;
        private readonly ITemplateSelectService _templateSelectService;

        public DataSheetModelGenerator(IStandardRepository standardRepository, ITemplateSelectService templateSelectService)
        {
            _standardRepository = standardRepository;
            _templateSelectService = templateSelectService;
        }

        /// <summary>
        /// 按照testPointParams的ParamSet进行分组，生成DataSheetModel列表
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public async Task<List<DataSheetModel>> GenerateAsync(List<ConditionPool> pools, CheckListItem item,CancellationToken ct) 
        {
            var packages = PackByParamSet(item);

            var models = new List<DataSheetModel>(packages.Count);

            foreach (var package in packages)
            {
                var target = package.TestPointIds.ToHashSet();

                var pool = pools.FirstOrDefault(p =>
                    p.TestPoints != null &&
                    target.All(x => p.TestPoints.Contains(x)));

                if (pool == null) continue; // 防御性编程，避免后续空引用

                //-----------------------------------------------------------------
                //用运行时克隆字典避免并发安全
                var runtimeConditions = new Dictionary<string, object?>(pool.Conditions);

                var options = new JsonSerializerOptions { Converters = { new TypeConverter() } };
               
                var paramJson = package.ParamSet?.Values != null
                    ? System.Text.Json.JsonSerializer.Serialize(package.ParamSet.Values, options)
                    : "{}";

                var standardIds = item.StandardIds
                    .Where(id => id != null)
                    .Select(id => new StandardId(id.Value))
                    .ToList();

                // 2. 查库批量获取 Standard 实体
                var standards = await _standardRepository.GetByIdsAsync(standardIds, ct);

                // 3. 提取名称并用 ";" 分割，若查不到则降级显示 Id
                // 排序后再拼接: TestMethod 会进入 TemplateIndex 匹配条件(MatchesAll 逐字比对),
                // 若依赖 DB 返回顺序, 同一组标准可能拼出 "A;B" 或 "B;A" 造成索引匹配漂移
                var testMethod = standards.Any()
                    ? string.Join(";", standards
                        .OrderBy(s => s.Id.Value, StringComparer.OrdinalIgnoreCase)
                        .Select(s => s.StandardCode ?? s.StandardCodeNameEn ?? s.Id.Value))
                    : string.Join(";", item.StandardIds.Select(id => id?.Value ?? string.Empty));

                // 合并 package.ParamSet
                if (package.ParamSet?.Values != null)
                {
                    foreach (var kv in package.ParamSet.Values.ToDictionary())
                    {
                        runtimeConditions[kv.Key] = kv.Value;
                    }
                }

                // 追加 TestItemId / TestMethod
                runtimeConditions["TestItemId"] = item.TestItemId!.Value;
                runtimeConditions["TestMethod"] = testMethod;

                //-----------------------------------------------------------------

                //显示指定初筛key，要求condition把testitem、method、state等其他传入保存
                var template = _templateSelectService.FindByIndex(runtimeConditions, preFilterKey: "TestItemId");

                // 需根据 DataSheetModel 的实际构造函数或初始化方式进行调整
                var model = new DataSheetModel
                {
                    ReportNumber = pool.Conditions.TryGetValue("ReportNo", out var rnVal)
                    ? ExtractString(rnVal)
                    : string.Empty,

                    SheetCount = pool.Conditions.TryGetValue("Copy", out var cpVal)
                    ? ExtractInt(cpVal, 1)
                    : 1,

                    DataAreaCount = CalculateDataAreaCount(item,package),

                    TemplateUrl = template.GetTemplateUrl(),

                    TestItem = item.TestItemId,

                    TestMethod = testMethod,

                    TestCondition = TextReplaceHelper.FillTemplate(
                        template.FindTextTemplate(pool.Conditions).Text,
                        paramJson),

                    SampleMap = await CalcuteSampleMapAsync(item, package, template ,ExpandSampleArray(item, package)),
                      
                    AfterWashMap = await CalculateAfterWashMapAsync(item, package, template, ExpandAfterWashArray(item, package))
                };
                models.Add(model);
            }

            return models;
        }

        /// <summary>
        /// 参数包分化策略
        /// 计算每个ParamSet对应的测试点ID列表，返回TestPointPackage列表
        /// 注意参数包只是为了分配测点，所以只需要考虑测点ID即可
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public IReadOnlyList<TestPointPackage> PackByParamSet(CheckListItem item)
        {
            if (item.TestPointParams == null || item.TestPointParams.Count == 0)
                return Array.Empty<TestPointPackage>();

            //需要把ParamSet中的水洗次数WashCycle去除后再进行比较，不然会影响比较的准确性
            // 1. 分组键：排除 WashCycle 后的 ParamSet 用于判等
            var packages = item.TestPointParams
                .GroupBy(kv =>
                {     
                    if (kv.Value?.Values == null)
                        return "{}"; // 空值统一返回空JSON字符串    

                    // 克隆 Values 字典，若包含 WashCycle 则移除，避免影响参数合并的准确性
                    var filteredValues = new Dictionary<string, object>();
                    foreach (var v in kv.Value.Values)
                    {
                        if ("WashCycle".Equals(v.Key, StringComparison.OrdinalIgnoreCase))
                            continue;

                        filteredValues[v.Key] = v.Value is JsonElement je
                            ? je.GetRawText() ?? string.Empty   // 拿原始 JSON 文本，若为null则降级为空字符串
                            : v.Value ?? string.Empty;          // 若v.Value为null，同样降级为空字符串
                    }
                    var key = JsonConvert.SerializeObject(filteredValues);
                    return key;
                })
                // 2. 保留原始的 ParamSet (包含 WashCycle)，仅用分组键聚合测点
                .Select(g => new TestPointPackage(
                    TestPointIds: g.Select(x => x.Key).OrderBy(k => k).ToList(),
                    ParamSet: g.First().Value)) // 使用原始 Value
                .OrderBy(p => p.TestPointIds.First())
                .ToList();

            return packages;
        }

        /// <summary>
        /// dataArea计算策略，兼容涵盖水洗次数的情况
        /// </summary>
        /// <param name="item"></param>
        /// <param name="package"></param>
        /// <returns></returns>
        public int CalculateDataAreaCount(CheckListItem item, TestPointPackage package)
        {
            //如果非水洗扩展，那么就直接认定，Area个数未package.TestPointIds.Count,
            //水洗次数分两种：一种是一个测点对应一个水洗次数，另外一种是一种是一个测点对应多个水洗次数
            //暂定水洗次数字段为WashCycle，格式：1    or   1-2-32-45这样的分割数代表水洗1次2次32次45次

            //故dataArea的需求个数为package.TestPointIds的每个测点在item.TestPointParams中对应的水洗次数之和
            //因为在一个参数包中水洗次数不同不影响参数合并

            if (item.TestPointParams == null || item.TestPointParams.Count == 0)
                return package.TestPointIds.Count;

            int totalCount = 0;

            foreach (var testPointId in package.TestPointIds)
            {
                if (!item.TestPointParams.TryGetValue(testPointId, out var paramSet) || paramSet?.Values == null)
                {
                    totalCount += 1; // 无参数配置，默认1个Area
                    continue;
                }

                // 尝试从 ParamSet.Values 中获取 WashCycle 字段
                if (paramSet.Values.TryGetValue("WashCycle", out var washCycleObj) && washCycleObj is string washCycleStr)
                {
                    // 格式如 "1-2-32-45"，按 '-' 分割代表水洗1次、2次、32次、45次
                    var washCount = washCycleStr.Split('-', StringSplitOptions.RemoveEmptyEntries).Length;
                    totalCount += washCount > 0 ? washCount : 1;
                }
                else
                {
                    totalCount += 1; // 非水洗扩展，每个测点占1个Area
                }
            }

            return totalCount;
        }

        /// <summary>
        /// 根据水洗次数扩展测点数组
        /// </summary>
        public string[] ExpandSampleArray(CheckListItem item, TestPointPackage package)
        {
            if (item.TestPointParams == null || item.TestPointParams.Count == 0)
                return package.TestPointIds.ToArray();

            var expandedSamples = new List<string>();

            foreach (var testPointId in package.TestPointIds)
            {
                if (!item.TestPointParams.TryGetValue(testPointId, out var paramSet) || paramSet?.Values == null)
                {
                    expandedSamples.Add(testPointId); // 无参数配置，默认1次
                    continue;
                }

                if (paramSet.Values.TryGetValue("WashCycle", out var washCycleObj) && washCycleObj is string washCycleStr)
                {
                    var washCount = washCycleStr.Split('-', StringSplitOptions.RemoveEmptyEntries).Length;
                    int repeatCount = washCount > 0 ? washCount : 1;

                    for (int i = 0; i < repeatCount; i++)
                    {
                        expandedSamples.Add(testPointId); // 按水洗次数重复添加测点
                    }
                }
                else
                {
                    expandedSamples.Add(testPointId); // 非水洗扩展，添加1次
                }
            }

            return expandedSamples.ToArray();
        }

        public async Task<SampleMap> CalcuteSampleMapAsync(CheckListItem item, TestPointPackage package, Template template , string[] samplemap) 
        {
            //获取Template对应的TemplateStructure数据，
            //获取 SampleDataAreaCount 字段的值（模板预留的试样编号格数）
            //例如 SampleDataAreaCount = 3，可以得出对应的书签为 [Sample_data_1, Sample_data_2, Sample_data_3]
            //注：TemplateStructure.SampleResultAreaCount（试验结果区）当前未参与本模型的计算
            //这和书签的内容对应，故可以根据之前的水洗扩展map对对应的samplemap进行赋值，具体规则为：
            //      1.如果实际的samplemap的个数大于 SampleDataAreaCount，多出的 sample_data_x 需由模板引擎复制 data 表后再填充，isAppend 记录是否需要复制data表格
            //      2.如果实际的samplemap的个数小于等于 SampleDataAreaCount，直接赋值，isAppend 记录 false
            //      3.例外：如果没有sample，跳过即可
            // 1. 获取模板信息（假设已从数据库获取）

            int sampleDataCount = template.TemplateStructure.SampleDataAreaCount;

            // 例外：没有测点（或展开后为空）时无需任何 sample 映射。
            // 注：这里不再调用 CalculateDataAreaCount 做二次计算 —— 它与展开结果长度恒等，
            // 两套同构算法并存会在日后一处口径变更时悄悄分叉，故统一以展开结果为准。
            if (samplemap == null || samplemap.Length == 0)
            {
                return new SampleMap();
            }

            // 生成 SampleMetaData 字典：按展开后的测点顺序铺平，Sample_data_{i+1} 对应第 i 个测点
            var sampleMap = new SampleMap
            {
                SampleMetaData = new Dictionary<string, string>(samplemap.Length)
            };

            for (int i = 0; i < samplemap.Length; i++)
            {
                sampleMap.SampleMetaData[$"Sample_data_{i + 1}"] = samplemap[i];
            }

            // 需求口径：测点数「超过」模板预留格数才需要复制整块 data 表（相等时刚好填满，不复制）。
            // 另：SampleDataAreaCount 未配置时为 0（TemplateStructure 无初始化器，工厂只校验 < 0），
            // 此时若直接比较会恒为 true，故必须先要求模板确实配置了 sample 区。
            sampleMap.IsAppend = sampleDataCount > 0 && samplemap.Length > sampleDataCount;

            return sampleMap;
        }

        /// <summary>
        /// 计算AfterWashMap
        /// </summary>
        public async Task<AfterWashMap> CalculateAfterWashMapAsync(CheckListItem item, TestPointPackage package, Template template ,string[] afterWashArray)
        {
            // 1. 获取模板信息（假设已从数据库获取）
            int afterWashDataCount = template.TemplateStructure.AfterWashDataCount; // templateInfo.AfterWashDataCount;

            if ( afterWashDataCount <= 0)
            {
                return new AfterWashMap();
            }

            // 2. 例外：没有水洗数据（或展开后为空）时无需任何映射。
            // 注：不再调用 CalculateDataAreaCount 做二次同构计算，统一以展开结果为准。
            if (afterWashArray == null || afterWashArray.Length == 0)
            {
                return new AfterWashMap();
            }

            // 3. 生成 AfterWashMetaData 字典：After_wash_data_{i+1} 对应第 i 个水洗次数
            var afterWashMap = new AfterWashMap
            {
                AfterWashMetaData = new Dictionary<string, string>(afterWashArray.Length)
            };

            for (int i = 0; i < afterWashArray.Length; i++)
            {
                afterWashMap.AfterWashMetaData[$"After_wash_data_{i + 1}"] = afterWashArray[i];
            }

            // 4. 需求口径：水洗格数「超过」模板预留格数才需要复制整块表（相等时刚好填满，不复制）
            afterWashMap.IsAppend = afterWashDataCount > 0 && afterWashArray.Length > afterWashDataCount;

            return afterWashMap;
        }

        /// <summary>
        /// 根据水洗次数扩展AfterWash数组
        /// </summary>
        public string[] ExpandAfterWashArray(CheckListItem item, TestPointPackage package)
        {
            if (item.TestPointParams == null || item.TestPointParams.Count == 0)
            {
                // 与下方兜底口径一致：无参数配置视为水洗 1 次
                // （原实现误返回测点 ID 数组，会把测点号填进水洗次数列）
                return Enumerable.Repeat("1", package.TestPointIds.Count).ToArray();
            }

            var expandedAfterWashes = new List<string>();

            foreach (var testPointId in package.TestPointIds)
            {
                if (!item.TestPointParams.TryGetValue(testPointId, out var paramSet) || paramSet?.Values == null)
                {
                    expandedAfterWashes.Add("1"); // 无参数配置，默认水洗 1 次
                    continue;
                }

                if (paramSet.Values.TryGetValue("WashCycle", out var washCycleObj) && washCycleObj is string washCycleStr)
                {
                    // 解析水洗次数字符串，如 "1-23-32-45"
                    var washNumbers = washCycleStr.Split('-', StringSplitOptions.RemoveEmptyEntries);

                    // 为每个水洗次数添加对应的值
                    foreach (var washNumber in washNumbers)
                    {
                        expandedAfterWashes.Add(washNumber.Trim());
                    }
                }
                else
                {
                    expandedAfterWashes.Add("1"); // 非水洗扩展，默认水洗 1 次
                }
            }

            return expandedAfterWashes.ToArray();
        }

        private static string ExtractString(object? raw)
        {
            if (raw == null) return string.Empty;

            switch (raw)
            {
                case JsonElement je:
                    return je.ValueKind switch
                    {
                        JsonValueKind.String => je.GetString() ?? string.Empty,
                        JsonValueKind.Number => je.GetRawText(),
                        JsonValueKind.True => "true",
                        JsonValueKind.False => "false",
                        JsonValueKind.Null => string.Empty,
                        JsonValueKind.Undefined => string.Empty,
                        _ => je.GetRawText()
                    };

                case ParamValue pv:
                    return pv.Value?.ToString() ?? string.Empty;

                default:
                    return raw.ToString() ?? string.Empty;
            }
        }

        private static int ExtractInt(object? raw, int defaultValue = 1)
        {
            if (raw == null) return defaultValue;

            switch (raw)
            {
                case JsonElement je:
                    if (je.ValueKind == JsonValueKind.Number)
                        return je.TryGetInt32(out var n) ? n : defaultValue;
                    if (je.ValueKind == JsonValueKind.String
                        && int.TryParse(je.GetString(), out var s))
                        return s;
                    return defaultValue;

                case ParamValue pv:
                    return ExtractInt(pv.Value, defaultValue);

                case int i: return i;
                case long l: return (int)l;
                case double d: return (int)d;

                default:
                    return int.TryParse(raw.ToString(), out var parsed) ? parsed : defaultValue;
            }
        }
    }
}
