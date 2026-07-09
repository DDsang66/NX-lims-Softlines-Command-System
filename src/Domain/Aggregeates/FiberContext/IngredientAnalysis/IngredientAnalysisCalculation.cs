using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.ValueObj;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis
{
    public sealed class IngredientAnalysisCalculation : IAggregateRoot
    {
        private List<FiberComponent> _components = new();
        public long Id { get; private set; } /*AnalysisId*/
        public string ReportNo { get; private set; } = string.Empty;//报告流水号
        public string Buyer { get; private set; } = string.Empty;//买家
        public List<string> Methods { get; private set; } = new();
        public IReadOnlyList<FiberComponent> Components => _components.AsReadOnly();
        public RemarkLabel RemarkGroup { get; private set; } = new();
        public AnalysisType Type { get; private set; } // 枚举：单组分/多组分
        public AnalysisResult Result { get; private set; } = AnalysisResult.Empty();//字典映射
        private IReadOnlyDictionary<string, decimal> _moistureRegainMap = new Dictionary<string, decimal>();

        /// <summary>
        /// 实体创建工厂方法，包含领域验证逻辑
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reportNo"></param>
        /// <param name="buyer"></param>
        /// <param name="methods"></param>
        /// <param name="type"></param>
        /// <param name="components"></param>
        /// <returns></returns>
        /// <exception cref="DomainException"></exception>
        public static IngredientAnalysisCalculation Create(
                long id,
                string reportNo,
                string buyer,
                List<string> methods,
                AnalysisType type,
                List<FiberComponent> components,
                RemarkLabel? remarkLabel = null)
        {
            // 领域验证
            if (id <= 0) throw new ArgumentException("Id is required");
            if (string.IsNullOrWhiteSpace(reportNo)) throw new ArgumentException("RepoNo. is required");
            if (methods == null || !methods.Any()) throw new ArgumentException("Methods are required");
            if (components == null || !components.Any()) throw new ArgumentException("至少包含一组分数据");
            //等等

            return new IngredientAnalysisCalculation
            {
                Id = id,
                ReportNo = reportNo,
                Buyer = buyer,
                Methods = methods,
                Type = type,
                _components = components ?? new List<FiberComponent>(),
                RemarkGroup = remarkLabel ?? new RemarkLabel()
            };
        }

        /*------------------------------------------计算逻辑------------------------------------------------------------------------------*/

        /// <summary>
        /// 计算逻辑
        /// </summary>
        public AnalysisResult Calculate(IReadOnlyDictionary<string, decimal>? moistureRegainMap = null)
        {
            _moistureRegainMap = moistureRegainMap ?? new Dictionary<string, decimal>();

            // 1) 基础参数
            var result = AnalysisResult.Empty()
                .WithBasicParams(ReportNo, Buyer, DateTime.Now, Methods)
                .WithComponentType(Type);

            // 2) 根据分析类型执行对应计算策略
            var calculatedFiberResult = Type switch
            {
                AnalysisType.Single => CalculateSingleComponents(
                    Components.OfType<SingleFiberComponent>()),

                AnalysisType.Multiple => CalculateMultipleComponents(
                    Components.OfType<DissolvedFiberComponent>(),
                    Components.OfType<SplittingFiberComponent>()),

                _ => throw new NotSupportedException($"不支持的成分分析类型: {Type}")
            };

            //计算calculatedFiberResult中每个成分列Rate
            calculatedFiberResult = CalculateRates(calculatedFiberResult);

            // 3) 计算实际成分数（多组分需要统计 FiberRows 中的成分行）
            var actualComponentCount = Type == AnalysisType.Multiple
                ? calculatedFiberResult
                    .OfType<MultiCalculatedFiberItem>()
                    .SelectMany(m => m.MultiFiberRowUnits ?? new List<MultiFiberRowUnit>())
                    .Count(r => r.Section == "/" && !string.IsNullOrWhiteSpace(r.Sum))
                : calculatedFiberResult.Count;

            result = result.WithAnalysisItems(calculatedFiberResult, actualComponentCount);

            // 3.5) 设备选型（对应 Excel L23/O23/R23/L24/O24）
            var orderedFiberNames = GetOrderedFiberNames();
            var equipment = SelectEquipment(orderedFiberNames.Count, orderedFiberNames);
            result = result.WithEquipment(equipment);

            // 3.6) 自动拼接 Methods（对应 Excel L4 公式）
            var selectedStandard = Methods.FirstOrDefault() ?? string.Empty;
            var methodString = BuildMethodString(selectedStandard, orderedFiberNames);
            result = result.WithMethods(methodString);

            // 3.7) 燃烧法分类（对应 ISO 11827 Table A.1）
            result = result.WithBurningTest(orderedFiberNames);

            // 4) 计算标签/备注
            var calculatedRemarkResult = GenerateRecommendedLabel(RemarkGroup, calculatedFiberResult);

            result = result.WithRemarkLabelResult(calculatedRemarkResult);

            // 5) 保存聚合根状态
            Result = result;

            return result;
        }

        /// <summary>
        /// 单组分计算
        /// </summary>
        /// <param name="singles"></param>
        /// <returns></returns>
        private List<CalculatedFiberResult> CalculateSingleComponents(IEnumerable<SingleFiberComponent> singles)
        {
            var qualitative = GetFiberQualitative();

            return singles.Select(component => new SingleCalculatedFiberItem
            {
                Qualitative = qualitative,
                Reagent = ReagentCalculateMethod(qualitative),
                FiberName = component.FiberName,
                Sample = component.Sample,
                GSMTrail1 = Convert.ToDecimal(component.GSMTrail1),
                Rate = 100m    // 单组分固定为100%
            }).Cast<CalculatedFiberResult>().ToList();
        }

        /// <summary>
        /// 多组分计算
        /// </summary>
        /// <param name="dissolveds"></param>
        /// <param name="splittings"></param>
        /// <returns></returns>
        private List<CalculatedFiberResult> CalculateMultipleComponents(IEnumerable<DissolvedFiberComponent> dissolveds, IEnumerable<SplittingFiberComponent> splittings)
        {
            // 1) 计算样品总干重（所有拆分列 + 所有溶解列的原始干重）
            //    每个溶解组独立 fallback：若 OriginalGSM 未填，取该组第一溶解行 GSM
            var dissolvedT1 = dissolveds.Sum(d => d.OriginalGSMTrail1 > 0
                ? (decimal)d.OriginalGSMTrail1
                : (decimal)(d.DissolutionUnits?.FirstOrDefault()?.GSMTrail1 ?? 0));
            var dissolvedT2 = dissolveds.Sum(d => d.OriginalGSMTrail2 > 0
                ? (decimal)d.OriginalGSMTrail2
                : (decimal)(d.DissolutionUnits?.FirstOrDefault()?.GSMTrail2 ?? 0));
            var totalGSMTrail1 = splittings.Sum(s => (decimal)s.GSMTrail1) + dissolvedT1;
            var totalGSMTrail2 = splittings.Sum(s => (decimal)s.GSMTrail2) + dissolvedT2;

            var splittingUnits = CalculateSplittingUnits(splittings, totalGSMTrail1, totalGSMTrail2);

            // 计算溶解列的起始 Yarn 下标（承接拆分列的最后一个）
            // 拆分列每个组件对应一个 DissolutionUnit，所以数量 = 最后一个 Yarn # 编号
            var startYarnIndex = splittingUnits.Count;

            // 计算溶解列的单元
            var dissolvedUnits = CalculateDissolvedUnits(dissolveds, totalGSMTrail1, totalGSMTrail2, startYarnIndex);

            // 合并所有单元（拆分列 + 溶解列）
            var allUnits = splittingUnits.Concat(dissolvedUnits).ToList();

            var qualitative = GetFiberQualitative();

            // 只创建一个 MultiCalculatedFiberItem 包含所有单元，避免每个溶解组重复复制 allUnits 导致百分比被重复计算
            var item = new MultiCalculatedFiberItem
            {
                Qualitative = qualitative,
                Reagent = ReagentCalculateMethod(qualitative),
                GSMTrail1 = totalGSMTrail1,
                GSMTrail2 = totalGSMTrail2,
                MultiFiberRowUnits = allUnits,
                Sample = dissolveds.FirstOrDefault()?.Sample ?? string.Empty
            };
            return new List<CalculatedFiberResult> { item };
        }

        /// <summary>
        /// 拆分列单元结果
        /// </summary>
        /// <param name="splittings"></param>
        /// <param name="totalGSMTrail1"></param>
        /// <param name="totalGSMTrail2"></param>
        /// <returns></returns>
        private List<MultiFiberRowUnit> CalculateSplittingUnits(IEnumerable<SplittingFiberComponent> splittings, decimal totalGSMTrail1, decimal totalGSMTrail2)
        {
            var units = new List<MultiFiberRowUnit>();
            int yarnIndex = 1;

            foreach (var s in splittings.OrderBy(x => x.SplittingOrder))
            {
                var rateTrail1 = totalGSMTrail1 == 0 ? 0 : (decimal)s.GSMTrail1 / totalGSMTrail1 * 100;

                var rateTrail2 = totalGSMTrail2 == 0 ? 0 : (decimal)s.GSMTrail2 / totalGSMTrail2 * 100;

                var avg = (rateTrail1 + rateTrail2) / 2;

                units.Add(new MultiFiberRowUnit
                {
                    Section = $"{{Yarn #{yarnIndex}}}",
                    Sum = s.FiberName,
                    GSMTrail1 = (decimal)s.GSMTrail1,
                    GSMTrail2 = (decimal)s.GSMTrail2,
                    RateTrail1 = rateTrail1,
                    RateTrail2 = rateTrail2,
                    Avg = avg,
                    Correct = 1,
                    MoistureRegain = 0,  // 拆分法无回潮率概念，或从配置取
                    Rate = 0             // 暂不计算
                });

                yarnIndex++;
            }

            return units;
        }

        /// <summary>
        /// 计算溶解列的单元结果
        /// 规则：
        /// 1. 每个溶解组共享相同的 Section 下标
        /// 2. 第1行为起始行：Sum=所有成分缩写拼接，Rate=OriginalGSMTrail/total
        /// 3. 中间行（非最后成分）：Rate=(当前成分重量-下一成分重量)/total（差值=被溶解掉的量）
        /// 4. 最后一行：Rate=当前成分重量/total（剩余的就是它自己）
        /// </summary>
        private List<MultiFiberRowUnit> CalculateDissolvedUnits(IEnumerable<DissolvedFiberComponent> dissolveds,decimal totalGSMTrail1,decimal totalGSMTrail2,int startIndex)
        {
            var units = new List<MultiFiberRowUnit>();

            int currentIndex = startIndex;

            foreach (var group in dissolveds)
            {
                currentIndex++;

                var section = $"{{Yarn #{currentIndex}}}";

                var groupUnits = group.DissolutionUnits.OrderBy(u => u.DissolutionStep).ToList();
                if (groupUnits.Count == 0) continue;  // 跳过空溶解组

                int componentCount = groupUnits.Count;

                // 起始行 GSM：有 originalGSM 则用它，否则用第一条 dissolved row 的 GSM
                var startGsm1 = group.OriginalGSMTrail1 > 0 ? (decimal)group.OriginalGSMTrail1 : (decimal)groupUnits[0].GSMTrail1;
                var startGsm2 = group.OriginalGSMTrail2 > 0 ? (decimal)group.OriginalGSMTrail2 : (decimal)groupUnits[0].GSMTrail2;

                // 计算所有成分缩写拼接
                var abbreviations = groupUnits.Select(u => GetFiberAbbreviation(u.FiberName)) .ToList();

                var combinedAbbreviation = string.Join("/", abbreviations);

                // 第1行：起始行（所有成分缩写拼接）
                units.Add(new MultiFiberRowUnit
                {
                    Section = section,
                    Sum = combinedAbbreviation,
                    GSMTrail1 = startGsm1,
                    GSMTrail2 = startGsm2,
                    RateTrail1 = SafeDivide(startGsm1, totalGSMTrail1),
                    RateTrail2 = SafeDivide(startGsm2, totalGSMTrail2),
                    Avg = (SafeDivide(startGsm1, totalGSMTrail1) + SafeDivide(startGsm2, totalGSMTrail2)) / 2,
                    Correct = 1,
                    MoistureRegain = 0,
                    Rate = 0
                });

                for (int i = 0; i < componentCount; i++)
                {
                    var current = groupUnits[i];
                    var isLast = i == componentCount - 1;

                    decimal ownGsm1, ownGsm2, curGsm1, curGsm2;
                    curGsm1 = (decimal)current.GSMTrail1;
                    curGsm2 = (decimal)current.GSMTrail2;

                    if (group.OriginalGSMTrail1 > 0)
                    {
                        // 有 originalGSM：行 GSM = 溶解后剩余，自身 = 上一步残 − 当前
                        decimal prevGsm1 = (i == 0) ? startGsm1 : (decimal)groupUnits[i - 1].GSMTrail1;
                        ownGsm1 = prevGsm1 - curGsm1;
                        decimal prevGsm2 = (i == 0) ? startGsm2 : (decimal)groupUnits[i - 1].GSMTrail2;
                        ownGsm2 = prevGsm2 - curGsm2;
                    }
                    else
                    {
                        // 无 originalGSM：行 GSM = 溶解前重量，自身 = 当前 − 下一
                        decimal nextGsm1 = !isLast ? (decimal)groupUnits[i + 1].GSMTrail1 : 0m;
                        ownGsm1 = curGsm1 - nextGsm1;
                        decimal nextGsm2 = !isLast ? (decimal)groupUnits[i + 1].GSMTrail2 : 0m;
                        ownGsm2 = curGsm2 - nextGsm2;
                    }

                    var rateTrail1 = SafeDivide(ownGsm1, totalGSMTrail1);
                    var rateTrail2 = SafeDivide(ownGsm2, totalGSMTrail2);

                    units.Add(new MultiFiberRowUnit
                    {
                        Section = section,
                        Sum = current.FiberName,
                        GSMTrail1 = curGsm1,
                        GSMTrail2 = curGsm2,
                        RateTrail1 = rateTrail1,
                        RateTrail2 = rateTrail2,
                        Avg = (rateTrail1 + rateTrail2) / 2,
                        Correct = 1,
                        MoistureRegain = 0,
                        Rate = 0
                    });
                }
            }

            return units;
        }

        /// <summary>
        /// 统一计算多组分各成分的Rate
        /// 公式：Rate = [(1+MR)*Correct/100]*Avg / Σ{[(1+MRi)*Correcti/100]*Avgi}
        /// </summary>
        private List<CalculatedFiberResult> CalculateRates(List<CalculatedFiberResult> calculatedFiberResult)
        {
            // 单组分直接返回（Rate已是100%）
            if (calculatedFiberResult.All(c => c is SingleCalculatedFiberItem))
            {
                return calculatedFiberResult;
            }

            // 提取所有需要计算Rate的MultiFiberRowUnit（成分行，排除起始汇总行）
            var allComponentRows = calculatedFiberResult
                .OfType<MultiCalculatedFiberItem>()
                .SelectMany(m => m.MultiFiberRowUnits ?? new List<MultiFiberRowUnit>())
                .Where(r => !string.IsNullOrWhiteSpace(r.Sum))
                .Where(r => !r.Sum.Contains('/'))  // 排除起始行的缩写（如 E/T）
                .Select(r =>
                {
                    // 从 MoistureRegainMap 查回潮率
                    var mr = LookupMoistureRegain(r.Sum);
                    return r with { MoistureRegain = mr };
                })
                .ToList();

            // 计算分母：所有成分的 [(1+MR)*Correct/100]*Avg 之和
            var denominator = allComponentRows.Sum(r =>
            {
                var factor = (1m + r.MoistureRegain / 100m) * r.Correct / 100m * r.Avg;
                return factor;
            });

            if (denominator == 0) return calculatedFiberResult;  // 避免除零

            // 更新每个MultiCalculatedFiberItem中的成分行Rate
            var updatedItems = calculatedFiberResult.Select(item =>
            {
                if (item is not MultiCalculatedFiberItem multi) return item;

                var updatedRows = multi.MultiFiberRowUnits?.Select(row =>
                {
                    // 起始汇总行（Sum包含/）不计算Rate
                    if (string.IsNullOrWhiteSpace(row.Sum) || row.Sum.Contains('/'))
                    {
                        return row;
                    }

                    // 查回潮率
                    var mr = LookupMoistureRegain(row.Sum);

                    // 计算分子
                    var numerator = (1m + mr / 100m) * row.Correct / 100m * row.Avg;

                    // 计算Rate并保留两位小数
                    var rate = Math.Round(numerator / denominator * 100m, 2, MidpointRounding.AwayFromZero);

                    return row with { MoistureRegain = mr, Rate = rate };

                }).ToList();

                return multi with { MultiFiberRowUnits = updatedRows };

            }).Cast<CalculatedFiberResult>().ToList();

            return updatedItems;
        }

        /// <summary>
        /// 计算结果、标签
        /// </summary>
        /// <param name="remarkLabel"></param>
        /// <param name="calculatedFiberResult"></param>
        /// <returns></returns>
        private CalculatedRemarkResult GenerateRecommendedLabel(RemarkLabel remarkLabel, List<CalculatedFiberResult> calculatedFiberResult) 
        {
            var result = new CalculatedRemarkResult {
                RecommendedLabel = new List<string>(remarkLabel.RecommendedLabel),
                ResultRemark = remarkLabel.ResultRemark,
                LabelRemark = remarkLabel.LabelRemark,
                JudgmentLabelRemark = remarkLabel.JudgmentLabelRemark,
                LanguageLabelRemark = remarkLabel.LanguageLabelRemark,
                DurabilityLabel = remarkLabel.DurabilityLabel,
                OtherLabel = remarkLabel.OtherLabel,
                Comprehensive = remarkLabel.Comprehensive,
                VerifyResult = remarkLabel.VerifyResult,
                FinalResult = remarkLabel.FinalResult,
                Results = CalculateFormattedResults(calculatedFiberResult, 1, "F1"),
                Recommendation = CalculateFormattedResults(calculatedFiberResult, 0, "F0")
            };

            return result;
        }

        /// <summary>
        /// 通用计算方法：提取成分、四舍五入、调整最大项、格式化
        /// </summary>
        /// <param name="calculatedFiberResult">计算结果</param>
        /// <param name="decimalPlaces">保留小数位（0=整数, 1=1位小数）</param>
        /// <param name="format">格式化字符串（F0 或 F1）</param>
        private List<string> CalculateFormattedResults(List<CalculatedFiberResult> calculatedFiberResult, int decimalPlaces, string format)
        {
            // 单组分：每个纤维固定 100%，不求和
            if (calculatedFiberResult.All(c => c is SingleCalculatedFiberItem))
            {
                return calculatedFiberResult
                    .OfType<SingleCalculatedFiberItem>()
                    .Select(s => $"{s.Sample}:\n100% {s.FiberName}")
                    .ToList();
            }

            // 1. 提取原始成分数据
            var rawComponents = ExtractComponents(calculatedFiberResult);

            // 2. 四舍五入
            var rounded = rawComponents
                .Select(c => new
                {
                    c.Name,
                    RoundedRate = Math.Round(c.Rate, decimalPlaces, MidpointRounding.AwayFromZero)
                })
                .ToList();

            // 3. 计算总和
            var sum = rounded.Sum(r => r.RoundedRate);

            // 4. 调整最大项（无论大于还是小于100）
            if (sum != 100m)
            {
                var diff = 100m - sum;  // 正数=需要加，负数=需要减
                var maxItem = rounded.OrderByDescending(r => r.RoundedRate).First();

                for (int i = 0; i < rounded.Count; i++)
                {
                    if (rounded[i].Name == maxItem.Name)
                    {
                        rounded[i] = new
                        {
                            rounded[i].Name,
                            RoundedRate = rounded[i].RoundedRate + diff  // 加或减差值
                        };
                        break;
                    }
                }
            }

            // 5. 格式化输出（按Rate从大到小排序）
            return rounded
                .OrderByDescending(r => r.RoundedRate)
                .Select(r => $"{r.RoundedRate.ToString(format)}% {r.Name}")
                .ToList();
        }

        /// <summary>
        /// 从计算结果中提取成分名称和原始Rate
        /// </summary>
        private List<(string Name, decimal Rate)> ExtractComponents(List<CalculatedFiberResult> calculatedFiberResult)
        {
            var components = new List<(string Name, decimal Rate)>();

            foreach (var item in calculatedFiberResult)
            {
                switch (item)
                {
                    case SingleCalculatedFiberItem single:
                        components.Add((single.FiberName, single.Rate));
                        break;

                    case MultiCalculatedFiberItem multi when multi.MultiFiberRowUnits != null:
                        var items = multi.MultiFiberRowUnits
                            .Where(r => !string.IsNullOrWhiteSpace(r.Sum))
                            .Where(r => !r.Sum.Contains('/'))  // 排除缩写拼接的起始行（如 E/T）
                            .Select(r => (r.Sum, r.Rate));
                        components.AddRange(items);
                        break;
                }
            }

            return components;
        }

        private decimal LookupMoistureRegain(string fiberName)
        {
            if (string.IsNullOrWhiteSpace(fiberName) || _moistureRegainMap.Count == 0)
                return 0m;

            if (_moistureRegainMap.TryGetValue(fiberName, out var exact))
                return exact;

            var match = _moistureRegainMap
                .FirstOrDefault(kv => string.Equals(kv.Key, fiberName, StringComparison.OrdinalIgnoreCase));
            return match.Value;
        }

        /// <summary>
        /// 安全除法：除数为0时返回0，避免异常
        /// </summary>
        private static decimal SafeDivide(decimal numerator, decimal denominator)
        {
            return denominator == 0 ? 0 : numerator / denominator * 100;  // 返回百分比
        }

        /// <summary>
        /// 获取纤维名称的缩写
        /// 规则：取首字母，或根据配置映射
        /// </summary>
        private static string GetFiberAbbreviation(string fiberName)
        {
            // 简单实现：取首字母
            // 需要从配置或数据库查询标准缩写
            return fiberName switch
            {
                "Cotton" => "C",
                "Polyester" => "T",
                "Spandex" => "E",
                "Nylon" => "N",
                "Wool" => "W",
                "Silk" => "S",
                "Linen" => "L",
                "Acrylic" => "A",
                _ => fiberName[..1].ToUpper()  // 默认取首字母
            };
        }

        /// <summary>
        /// 获取所有成分名称拼接
        /// </summary>
        /// <returns></returns>
        private string GetFiberQualitative()
        {
            var qualitative = Type switch
            {
                AnalysisType.Single => GetSingleFiberNames(),
                AnalysisType.Multiple => GetMultipleFiberNames(),
                _ => throw new NotSupportedException($"不支持的成分分析类型: {Type}")
            };
            return qualitative;
        }

        /// <summary>
        /// 单组分：从 _components 中提取纤维名称
        /// </summary>
        private string GetSingleFiberNames()
        {
            return string.Join("/",
                _components
                    .OfType<SingleFiberComponent>()
                    .Select(c => c.FiberName)
                    .Distinct());
        }

        /// <summary>
        /// 多组分：从 _components 中提取所有纤维名称
        /// </summary>
        private string GetMultipleFiberNames()
        {
            var dissolvedNames = _components
                .OfType<DissolvedFiberComponent>()
                .SelectMany(d => d.DissolutionUnits)
                .Select(u => u.FiberName)
                .Distinct();

            var splittingNames = _components
                .OfType<SplittingFiberComponent>()
                .Select(s => s.FiberName)
                .Distinct();

            return string.Join("/", dissolvedNames.Concat(splittingNames));
        }

        /*------------------------------------------Method 自动拼接------------------------------------------------------------------------*/

        private const string ISO_QUALITATIVE = "ISO/TR 11827:2012";
        private const string DIN_QUALITATIVE = "DIN CEN ISO/TR 11827:2019";
        private const string ISO1833_1 = "ISO1833-1:2020";
        private const string ISO1833_2 = "ISO1833-2:2020";
        private const string ISO1833_3 = "ISO1833-3:2020";
        private const string ISO1833_4 = "ISO1833-4:2023";
        private const string ISO1833_6 = "ISO1833-6:2018";
        private const string ISO1833_7 = "ISO1833-7:2017";
        private const string ISO1833_11 = "ISO1833-11:2017";
        private const string ISO1833_12 = "ISO1833-12:2020";
        private const string ISO1833_18 = "ISO1833-18:2020";
        private const string ISO1833_22 = "ISO1833-22:2020";
        private const string ISO1833_24 = "ISO1833-24:2010";

        private static readonly HashSet<string> DIN1833_D5x = new(StringComparer.OrdinalIgnoreCase)
        {
            "ISO1833-1:2020", "ISO1833-2:2020", "ISO1833-3:2020", "ISO1833-4:2023",
            "ISO1833-6:2018", "ISO1833-7:2017", "ISO1833-11:2017", "ISO1833-12:2020",
            "ISO1833-18:2020", "ISO1833-22:2020", "ISO1833-24:2010"
        };

        /// <summary>Excel L4: 根据标准体系和成分对自动拼接方法标准链</summary>
        private static string BuildMethodString(string standard, List<string> fibers)
        {
            if (string.IsNullOrWhiteSpace(standard)) return string.Empty;

            var isIso = standard.Equals("ISO1833", StringComparison.OrdinalIgnoreCase);
            var isDin = standard.Equals("DIN EN ISO 1833", StringComparison.OrdinalIgnoreCase);

            // 非 ISO/DIN：直接返回原值
            if (!isIso && !isDin) return standard;

            // 3 组分走 -2 号
            if (fibers.Count == 3)
                return isIso ? $"{ISO_QUALITATIVE} {ISO1833_2}"
                             : $"{DIN_QUALITATIVE} DIN EN ISO 1833-2:2020";

            // 从成分对查子标准号
            var subStandards = new List<string>();
            for (int i = 0; i < fibers.Count - 1; i++)
            {
                var s = LookupSubStandard(fibers[i], fibers[i + 1]);
                if (!string.IsNullOrEmpty(s) && !subStandards.Contains(s))
                    subStandards.Add(s);
            }

            // 有拆分列时加 -1
            var parts = new List<string>();
            if (isIso) parts.Add(ISO_QUALITATIVE);
            else parts.Add(DIN_QUALITATIVE);

            // -1 号（暂不判断拆分列，先不加）
            parts.AddRange(subStandards);

            // DIN 版：替换 ISO 前缀为 DIN EN
            if (isDin)
                parts = parts.Select(p => DIN1833_D5x.Contains(p)
                    ? "DIN EN " + p
                    : p).ToList();

            return string.Join(" ", parts);
        }

        /// <summary>对相邻成分对查表返回 ISO1833 子标准编号</summary>
        private static string LookupSubStandard(string first, string second)
        {
            var f = first.ToLowerInvariant();
            var s = second.ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;

            // rayon/modal/lyocell/cotton/cupro + elastane → -1
            if (IsCellulosic(f) && (s == "elastane" || s == "spandex"))
                return ISO1833_1;

            // Silk + wool/cashmere → -18
            if (f == "silk" && (s == "wool" || s == "cashmere"))
                return ISO1833_18;

            // wool/animal + any → -4
            if (IsAnimal(f))
                return ISO1833_4;

            // elastane + elastomultiester/polyester → -11
            if (IsElastane(f) && (s == "elastomultiester" || s == "polyester"))
                return ISO1833_11;

            // elastane + other → -12
            if (IsElastane(f))
                return ISO1833_12;

            // polyamide/nylon + any → -7
            if (f == "polyamide" || f == "nylon")
                return ISO1833_7;

            // acrylic + any → -12
            if (f == "acrylic")
                return ISO1833_12;

            // cellulosic + cotton/cellulosic → -6
            if (IsCellulosic(f) && IsCellulosicOrCotton(s))
                return ISO1833_6;

            // cellulosic/cotton + elastomultiester/polyester → -11
            if (IsCellulosicOrCotton(f) && (s == "elastomultiester" || s == "polyester"))
                return ISO1833_11;

            // cellulosic + linen/ramie → -22
            if (IsRayonType(f) && (s == "linen" || s == "ramie"))
                return ISO1833_22;

            // polyester + any → -24
            if (f == "polyester")
                return ISO1833_24;

            // acetate alone → -3
            if (f == "acetate")
                return ISO1833_3;

            return string.Empty;
        }

        private static bool IsAnimal(string f) => f == "wool" || f == "alpaca" || f == "cashmere" || f == "mohair" || f == "*animal" || f == "rabbit hair" || f == "silk";
        private static bool IsElastane(string f) => f == "elastane" || f == "spandex";
        private static bool IsCellulosic(string f) => f == "rayon" || f == "*re cellulose" || f == "viscose" || f == "modal" || f == "lyocell" || f == "cupro" || f == "cotton";
        private static bool IsCellulosicOrCotton(string f) => f == "cotton" || f == "hemp" || f == "paper" || IsCellulosic(f) || f == "linen" || f == "ramie" || f == "*cellulosic fiber";
        private static bool IsRayonType(string f) => f == "rayon" || f == "*re cellulose" || f == "viscose" || f == "modal" || f == "cupro" || f == "lyocell";

        /// <summary>
        /// 溶剂计算逻辑
        /// </summary>
        /// <param name="qualitative"></param>
        /// <returns></returns>
        private static string ReagentCalculateMethod(string qualitative)
        {
            if (string.IsNullOrWhiteSpace(qualitative)) return string.Empty;

            var rules = new (string Reagent, string[] Keywords)[]
            {
                ("NaClO",        new[]{"Wool","Alpaca","Mohair","Rabbit hair","Cashmere","Camel","Yak","Silk","Horse hair","Tussah","Tussah silk"}),
                ("20%HCl",       new[]{"Polyamide","Nylon","Vina","Vinylon","Vinylal"}),
                ("DMF",          new[]{"Acrylic","Modacrylic","Spandex","Elastane"}),
                ("59.5%H2SO4",   new[]{"Rayon","Viscose","Modal","Lyocell","Cupro"}),
                ("70%H2SO4",     new[]{"Cotton","Linen","Hemp","Ramie","Jute","Paper","Paper yarn","Kapok","Abaca"}),
                ("98%H2SO4",     new[]{"Polyester","Elastomultiester","Rubber","Elastodiene"}),
                ("Acetone",      new[]{"Acetate","Triacetate"})
            };

            var fibers = qualitative.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var matched = new HashSet<string>();

            foreach (var fiber in fibers)
            {
                var trimmed = fiber.Trim();
                foreach (var (reagent, keywords) in rules)
                {
                    if (keywords.Any(k => trimmed.Contains(k, StringComparison.OrdinalIgnoreCase)))
                        matched.Add(reagent);
                }
            }

            return string.Join(", ", matched);
        }


        /*------------------------------------------设备选型逻辑--------------------------------------------------------------------------*/

        // 设备编码常量（对应 Excel L23/O23/R23/L24/O24）
        private const string MICROSCOPE = "Microscope:SFL-NGB-EQP-056";
        private const string OVEN = "Oven:SFL-NGB-EQP-164";
        private const string BALANCE = "Balance:SFL-NGB-EQP-061";
        private const string WATER_BATH = "Water bath:SFL-NGB-EQP-046";
        private const string SHAKER = "Shaker:SFL-NGB-EQP-052";

        // Shaker 触发纤维（首成分为这些时需化学溶解）
        private static readonly HashSet<string> ShakerFirstFibers = new(StringComparer.OrdinalIgnoreCase)
        {
            "nylon", "polyamide", "wool", "silk"
        };

        // P130 规则：Polyester 前不允许的纤维
        private static readonly HashSet<string> ProhibitedBeforePolyester = new(StringComparer.OrdinalIgnoreCase)
        {
            "nylon", "polyamide", "wool", "silk", "acetate"
        };

        /// <summary>
        /// 设备选型主入口（对应 Excel L23/O23/R23/L24/O24）
        /// </summary>
        private EquipmentSelection SelectEquipment(int componentCount, List<string> orderedFiberNames)
        {
            return new EquipmentSelection
            {
                Microscope = SelectMandatory(componentCount, MICROSCOPE),       // L23
                Oven = SelectMandatory(componentCount, OVEN),                   // O23
                Balance = SelectMandatory(componentCount, BALANCE),             // R23
                WaterBath = SelectWaterBath(orderedFiberNames),                 // L24
                Shaker = SelectShaker(orderedFiberNames)                        // O24
            };
        }

        /// <summary>L23/O23/R23 — 多组分必用设备</summary>
        private static string SelectMandatory(int componentCount, string deviceCode)
        {
            return componentCount > 1 ? deviceCode : string.Empty;
        }

        /// <summary>L24 — 水浴设备选择（P130/P131 规则）</summary>
        private string SelectWaterBath(List<string> fibers)
        {
            if (fibers.Count < 2) return string.Empty;

            // P130: 任何相邻对中后者为 polyester 且前者非禁止纤维 → 水浴（优先）
            for (int i = 1; i < fibers.Count; i++)
            {
                if (fibers[i].Equals("polyester", StringComparison.OrdinalIgnoreCase)
                    && !ProhibitedBeforePolyester.Contains(fibers[i - 1]))
                {
                    return WATER_BATH;
                }
            }

            // P131: 任何相邻对中前者为 acrylic 且后者存在 → 水浴（回退）
            for (int i = 1; i < fibers.Count; i++)
            {
                if (fibers[i - 1].Equals("acrylic", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(fibers[i]))
                {
                    return WATER_BATH;
                }
            }

            return string.Empty;
        }

        /// <summary>O24 — 振荡器/化学溶解设备选择</summary>
        private string SelectShaker(List<string> fibers)
        {
            if (fibers.Count < 2) return string.Empty;

            // 第一成分为 nylon/polyamide/wool/silk 且有 ≥2 成分 → Shaker
            if (ShakerFirstFibers.Contains(fibers[0]))
            {
                return SHAKER;
            }

            // 有拆分列（等效 Excel P129=1 → T150>0）→ Shaker
            var hasSplitting = Components.OfType<SplittingFiberComponent>().Any();
            if (hasSplitting)
            {
                return SHAKER;
            }

            return string.Empty;
        }

        /// <summary>
        /// 从上到下提取所有纤维名称的有序列表
        /// 多组分：拆分列（按 SplittingOrder）→ 溶解列（每组按 DissolutionStep）
        /// </summary>
        private List<string> GetOrderedFiberNames()
        {
            if (Type == AnalysisType.Single)
            {
                return Components.OfType<SingleFiberComponent>()
                    .Select(c => c.FiberName)
                    .ToList();
            }

            var names = new List<string>();

            // 拆分列先
            foreach (var s in Components.OfType<SplittingFiberComponent>().OrderBy(s => s.SplittingOrder))
            {
                names.Add(s.FiberName);
            }

            // 溶解列后（每组按步骤排序）
            foreach (var d in Components.OfType<DissolvedFiberComponent>())
            {
                foreach (var unit in d.DissolutionUnits.OrderBy(u => u.DissolutionStep))
                {
                    names.Add(unit.FiberName);
                }
            }

            return names;
        }

        /*------------------------------------------计算逻辑------------------------------------------------------------------------------*/
    }
}