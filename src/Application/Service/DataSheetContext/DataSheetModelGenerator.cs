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
                var pool = pools.FirstOrDefault(p =>
                    p.TestPoints != null &&
                    p.TestPoints.SequenceEqual(package.TestPointIds));

                if (pool == null) continue; // 防御性编程，避免后续空引用

                //-----------------------------------------------------------------

                var options = new JsonSerializerOptions { Converters = { new TypeConverter() } };
               
                var paramJson = package.ParamSet?.Values != null
                    ? System.Text.Json.JsonSerializer.Serialize(package.ParamSet.Values, options)
                    : "{}";

                var standardIds = item.StandardIds
                    .Where(id => id != null)
                    .Select(id => new StandardId(id.Value))
                    .ToList();

                // 2. 查库批量获取 Standard 实体
                var standards = await _standardRepository.GetByIdsAsync(standardIds, CancellationToken.None);

                // 3. 提取名称并用 ";" 分割，若查不到则降级显示 Id
                var testMethod = standards.Any()
                    ? string.Join(";", standards.Select(s => s.StandardCode ?? s.StandardCodeNameEn ?? s.Id.Value))
                    : string.Join(";", item.StandardIds.Select(id => id?.Value ?? string.Empty));

                var paramCondition = package.ParamSet!.Values.ToDictionary();

                pool.Merge(package.ParamSet!.Values.ToDictionary()); // 临时全量condition,不进入数据库
                pool.Merge(new Dictionary<string, object?>
                {
                    { "TestItemId", item.TestItemId!.Value },
                    { "TestMethod", testMethod }
                });

                //-----------------------------------------------------------------

                //显示指定初筛key，要求condition把testitem、method、state等其他传入保存
                var template = _templateSelectService.FindByIndex(pool.Conditions, preFilterKey: "TestItemId");

                //var mockText = "Procedure No: {WashProcedure}; Using horizontal axis, front-loading type machie: Machine wash at {WashingTemperature} degree C with {WashLoad} kg total dry mass( {BallastType} + specimen) and {ReferenceDetergentsComposition}, {DryProcedure}, / Iron.";

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

                    //TemplateUrl = "DocxModel/Common_WET/WET_Dimensional_Change_Wasing_Fabric.docx",

                    TemplateUrl = template.GetTemplateUrl(),

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
                    Console.WriteLine($"[{kv.Key}] -> [{key}]");
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
            //获取SampleDataCount字段的值和SampleResultCount字段的值
            //例如 SampleDataCount = 3，可以得出对应的SampleDataMap为 [Sample_data_1, Sample_data_2, Sample_data_3]
            //这和书签的内容对应，故可以根据之前的水洗扩展map对对应的samplemap进行赋值，具体规则为：
            //      1.如果实际的samplemap的个数大于等于SampleDataCount，计算出而外的sample_data_x,并且进行赋值，isAppend记录是否需要复制data表格
            //      2.如果实际的samplemap的个数小于SampleDataCount，则直接赋值，并且isAppend记录false
            //      3.例外：如果没有sample，跳过即可
            // 1. 获取模板信息（假设已从数据库获取）

            int sampleDataCount = template.TemplateStructure.SampleDataAreaCount; // templateInfo.SampleDataCount;

            // 2. 计算当前参数包因水洗扩展所需的实际数据区域总数
            int actualDataAreaCount = CalculateDataAreaCount(item, package);

            // 3. 例外：如果没有sample，直接返回空Map
            if (actualDataAreaCount <= 0)
            {
                return new SampleMap();
            }

            // 4. 生成 SampleMetaData 字典
            var sampleMap = new SampleMap
            {
                SampleMetaData = new Dictionary<string, string>()
            };

            // 5. 处理测点映射
            var expandedSamples = ExpandSampleArray(item, package);

            for (int i = 0; i < expandedSamples.Length; i++)
            {
                string sampleKey = $"Sample_data_{i + 1}";
                string sampleValue = expandedSamples[i];
                sampleMap.SampleMetaData[sampleKey] = sampleValue;
            }

            // 6. 根据 实际数量 与 模板预设数量 的比较设置 IsAppend
            sampleMap.IsAppend = expandedSamples.Length >= sampleDataCount;

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

            // 2. 计算当前参数包因水洗扩展所需的实际数据区域总数
            int actualDataAreaCount = CalculateDataAreaCount(item, package);

            // 3. 例外：如果没有afterWash，直接返回空Map
            if (actualDataAreaCount <= 0)
            {
                return new AfterWashMap();
            }

            // 4. 生成 AfterWashMetaData 字典
            var afterWashMap = new AfterWashMap
            {
                AfterWashMetaData = new Dictionary<string, string>()
            };

            // 5. 处理测点映射
            var expandedAfterWashes = ExpandAfterWashArray(item, package);

            for (int i = 0; i < expandedAfterWashes.Length; i++)
            {
                string afterWashKey = $"After_wash_data_{i + 1}";
                string afterWashValue = expandedAfterWashes[i];
                afterWashMap.AfterWashMetaData[afterWashKey] = afterWashValue;
            }

            // 6. 根据 实际数量 与 模板预设数量 的比较设置 IsAppend
            afterWashMap.IsAppend = expandedAfterWashes.Length >= afterWashDataCount;

            return afterWashMap;
        }

        /// <summary>
        /// 根据水洗次数扩展AfterWash数组
        /// </summary>
        public string[] ExpandAfterWashArray(CheckListItem item, TestPointPackage package)
        {
            if (item.TestPointParams == null || item.TestPointParams.Count == 0)
                return package.TestPointIds.ToArray();

            var expandedAfterWashes = new List<string>();

            foreach (var testPointId in package.TestPointIds)
            {
                if (!item.TestPointParams.TryGetValue(testPointId, out var paramSet) || paramSet?.Values == null)
                {
                    expandedAfterWashes.Add("1"); // 无参数配置，默认0次
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
                    expandedAfterWashes.Add("1"); // 非水洗扩展，默认0次
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
