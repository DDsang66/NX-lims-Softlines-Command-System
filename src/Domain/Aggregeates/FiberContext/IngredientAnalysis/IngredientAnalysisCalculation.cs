using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis
{
    public sealed class IngredientAnalysisCalculation
    {
        private List<FiberComponent> _components = new();
        public long Id { get; private set; } /*AnalysisId*/
        public string ReportNo { get; private set; } = string.Empty;//报告流水号
        public string Buyer { get; private set; } = string.Empty;//买家
        public List<string> Methods { get; private set; } = new();

        /// <summary>
        /// **只算一份**时用的那个标准 = <see cref="Methods"/> 的第一个。
        ///
        /// **它的含义比"本次采用的标准"窄**：多标准记录由 <see cref="CalculatePerStandard"/> 按
        /// <see cref="StandardsToRender"/> 逐份计算。这个属性只服务"只出一份"的场合 ——
        /// <see cref="Calculate"/> 的默认路径、<see cref="Result"/>（= 第 1 份），
        /// 以及 CalculateAsync / CalculateByReportAsync 两条重算路径。
        ///
        /// 仍然**只在这一处推导**：Methods.FirstOrDefault() ?? string.Empty 全类仅此一份。
        /// 服务层不用它去查回潮率列 —— 回潮率是**按各自的标准**逐个查的。
        /// </summary>
        public string SelectedStandard => Methods.FirstOrDefault() ?? string.Empty;

        /// <summary>Methods 为空时的退化形态，复用同一个实例，避免每次访问都分配。</summary>
        private static readonly IReadOnlyList<string> EmptyStandard = new[] { string.Empty };

        /// <summary>
        /// 本次要出几份、各按哪个标准。
        ///
        /// <see cref="Methods"/> 非空时就是它本身 —— **顺序 = 前端勾选顺序，刻意保留**
        /// （勾选顺序是分析员的显式选择，不做规范化）。
        ///
        /// 为空时**退化成单个空标准**，而不是空列表：今天"method 为空的记录仍会去查一次 Iso 回潮率列"
        /// 这个行为要保住，空列表会把整条路径短路掉，那是回归。
        /// </summary>
        public IReadOnlyList<string> StandardsToRender =>
            Methods.Count > 0 ? Methods : EmptyStandard;

        /// <summary>
        /// 每个标准各算一份的结果，顺序与 <see cref="StandardsToRender"/> 一致。
        ///
        /// 只由 <see cref="CalculatePerStandard"/> 填充。<see cref="Calculate"/>（单份路径）
        /// **不碰它** —— 单标准记录走的仍是原来那条只算一份的代码路径。
        /// </summary>
        public IReadOnlyList<AnalysisResult> StandardResults { get; private set; }
            = Array.Empty<AnalysisResult>();

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
        /// 只算一份（= <see cref="SelectedStandard"/> 那一份）。
        /// </summary>
        /// <remarks>
        /// 正文抽到 <see cref="CalculateForStandard"/> 之后，这里只剩一行委托。
        /// **签名与行为逐字不变** —— 服务层两条重算路径与既有契约测试原样通过。
        /// </remarks>
        public AnalysisResult Calculate(IReadOnlyDictionary<string, decimal>? moistureRegainMap = null)
        {
            Result = CalculateForStandard(SelectedStandard, moistureRegainMap);
            return Result;
        }

        /// <summary>
        /// 逐标准各算一份。顺序 = <see cref="StandardsToRender"/>，每个标准取**它自己的**回潮率 map。
        /// </summary>
        /// <param name="mrByStandard">标准串 → 该标准的回潮率 map。</param>
        /// <remarks>
        /// 结果写进 <see cref="StandardResults"/>，<see cref="Result"/> 取第 1 份 ——
        /// 两条重算路径读的就是 <see cref="Result"/>，所以它们**只回第 1 个标准**
        /// （这两条路径不产 docx、没有可合并的产物，故不去补第 2..N 份）。
        /// **不做去重**：同一记录里两个完全相同的标准串会各出一份。前端 el-select multiple
        /// 不允许选重复值，生产走不到，不为它发明规则。
        /// 某标准在 <paramref name="mrByStandard"/> 里查不到时按**空 map** 算（回潮率 0），
        /// 与 <see cref="Calculate"/> 的既有默认值同口径。服务层是按 <see cref="StandardsToRender"/>
        /// 逐个查的，正常不会缺项。
        /// </remarks>
        public IReadOnlyList<AnalysisResult> CalculatePerStandard(
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, decimal>> mrByStandard)
        {
            var list = StandardsToRender
                .Select(std => CalculateForStandard(
                    std,
                    mrByStandard != null && mrByStandard.TryGetValue(std, out var m) ? m : null))
                .ToList();

            StandardResults = list;
            Result = list[0];
            return list;
        }

        /// <summary>
        /// 单个标准的计算正文 —— 从 <see cref="Calculate"/> 原样抽出的那一段。
        /// </summary>
        /// <remarks>
        /// **与抽出前的唯一差别**：方法链那一处把 <see cref="SelectedStandard"/> 换成了入参
        /// <paramref name="standard"/>，且不再写 <see cref="Result"/>（由调用方决定写不写）。其余逐字未动。
        /// </remarks>
        private AnalysisResult CalculateForStandard(
            string standard,
            IReadOnlyDictionary<string, decimal>? moistureRegainMap)
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
            // cellulosic fibre 追加额外显微镜
            if (orderedFiberNames.Any(f => f == "*cellulosic fibre" || f == "*Regenerated cellulose fibre"))
                equipment = equipment with { Microscope = string.IsNullOrEmpty(equipment.Microscope)
                    ? MICROSCOPE_CELLULOSIC
                    : equipment.Microscope + " / " + MICROSCOPE_CELLULOSIC };
            result = result.WithEquipment(equipment);

            // 3.6) 自动拼接 Methods（对应 Excel L4 公式）
            // 配对另走**槽位通道**（第三条通道），GetOrderedFiberNames() 一字不动 —— 它继续喂
            // 上面的设备选型、上面的显微镜追加、以及规则表里的三处 `*cellulosic fibre` 字面量判定。
            // 三条通道职责不同，不合并：扁平列表管"报告上印什么"，槽位管"拿哪些名字去查表"。
            var methodString = FiberStandardChainBuilder.BuildMethodString(
                standard, orderedFiberNames, GetOrderedFiberSlots());
            result = result.WithMethods(methodString);

            // 3.7) 燃烧法分类（对应 ISO 11827 Table A.1）
            result = result.WithBurningTest(orderedFiberNames);

            // 4) 计算标签/备注
            var calculatedRemarkResult = GenerateRecommendedLabel(RemarkGroup, calculatedFiberResult, standard);

            result = result.WithRemarkLabelResult(calculatedRemarkResult);

            // 5) 不再写 Result —— 由调用方决定（Calculate 写、CalculatePerStandard 取第 1 份写）。
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
                // Bicomponent: 父 GSM 取子行第一个的数据
                var actualGsm1 = s.BicomponentSubFibers.Count > 0
                    ? s.BicomponentSubFibers[0].GSMTrail1
                    : (decimal)s.GSMTrail1;
                var actualGsm2 = s.BicomponentSubFibers.Count > 0
                    ? s.BicomponentSubFibers[0].GSMTrail2
                    : (decimal)s.GSMTrail2;

                var rateTrail1 = totalGSMTrail1 == 0 ? 0 : actualGsm1 / totalGSMTrail1 * 100;
                var rateTrail2 = totalGSMTrail2 == 0 ? 0 : actualGsm2 / totalGSMTrail2 * 100;
                var avg = (rateTrail1 + rateTrail2) / 2;

                units.Add(new MultiFiberRowUnit
                {
                    Section = $"{{Yarn #{yarnIndex}}}",
                    Sum = s.FiberName,
                    GSMTrail1 = actualGsm1,
                    GSMTrail2 = actualGsm2,
                    RateTrail1 = rateTrail1,
                    RateTrail2 = rateTrail2,
                    Avg = avg,
                    Correct = 1,
                    MoistureRegain = 0,
                    Rate = 0,
                    CellulosicSubFibers = s.CellulosicSubFibers ?? new(),
                    BicomponentSubFibers = s.BicomponentSubFibers ?? new()
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

                // 第1行：起始行（所有成分缩写拼接）— 多组分才需要（单组分缩写不含'/'会被误判为成分）
                if (componentCount > 1)
                {
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
                }

                for (int i = 0; i < componentCount; i++)
                {
                    var current = groupUnits[i];
                    var isLast = i == componentCount - 1;

                    decimal ownGsm1, ownGsm2, curGsm1, curGsm2;
                    curGsm1 = current.BicomponentSubFibers.Count > 0
                        ? current.BicomponentSubFibers[0].GSMTrail1
                        : (decimal)current.GSMTrail1;
                    curGsm2 = current.BicomponentSubFibers.Count > 0
                        ? current.BicomponentSubFibers[0].GSMTrail2
                        : (decimal)current.GSMTrail2;

                    // own = 当前行 - 下一行（差值 = 被溶解掉的量）
                    // 最后一行 own = 当前行自身
                    decimal nextGsm1 = !isLast ? (decimal)groupUnits[i + 1].GSMTrail1 : 0m;
                    ownGsm1 = curGsm1 - nextGsm1;
                    decimal nextGsm2 = !isLast ? (decimal)groupUnits[i + 1].GSMTrail2 : 0m;
                    ownGsm2 = curGsm2 - nextGsm2;

                    // rate = (own / firstRowGsm) * (startGsm / total) * 100
                    var firstRowGsm1 = (decimal)groupUnits[0].GSMTrail1;
                    var firstRowGsm2 = (decimal)groupUnits[0].GSMTrail2;
                    var rateTrail1 = totalGSMTrail1 == 0 || firstRowGsm1 == 0
                        ? 0m : ownGsm1 / firstRowGsm1 * (startGsm1 / totalGSMTrail1) * 100m;
                    var rateTrail2 = totalGSMTrail2 == 0 || firstRowGsm2 == 0
                        ? 0m : ownGsm2 / firstRowGsm2 * (startGsm2 / totalGSMTrail2) * 100m;

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
                        Rate = 0,
                        CellulosicSubFibers = current.CellulosicSubFibers ?? new(),
                        BicomponentSubFibers = current.BicomponentSubFibers ?? new()
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

                    // 计算Rate（不取整，保留全精度，最终格式化时统一取整）
                    var rate = numerator / denominator * 100m;

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
        /// <param name="standard">
        /// 本次这一份用的标准。**必须是入参、不能读 <see cref="Methods"/>** ——
        /// 多标准记录里两份共用同一个 <see cref="Methods"/> 列表，读它会让第 2 份沿用第 1 份的口径。
        /// </param>
        /// <returns></returns>
        private CalculatedRemarkResult GenerateRecommendedLabel(
            RemarkLabel remarkLabel, List<CalculatedFiberResult> calculatedFiberResult, string standard)
        {
            // AATCC 美标：原始 Rate < 5% 且不在豁免名单里的纤维 → 合并为 "other fiber(s)"
            // （规则与豁免见 CalculateFormattedResults 与 IsNamedBelowFivePercent）。
            // 必须按**这一份自己的**标准判：读 Methods[0] 的话，单标准下与入参同值、行为不变，
            // 多标准下却会让 ISO 段拿到 AATCC 的合并口径（或反过来）。
            var isAatcc = standard?.StartsWith("AATCC", StringComparison.OrdinalIgnoreCase) == true;

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
                Recommendation = CalculateFormattedResults(calculatedFiberResult, 0, "F0", isAatcc)
            };

            // Bicomponent/Biconstituent 格式化后处理
            result = result with
            {
                Results = PostProcessBicomponent(result.Results, calculatedFiberResult, "F1"),
                Recommendation = PostProcessBicomponent(result.Recommendation, calculatedFiberResult, "F0")
            };

            return result;
        }

        /// <summary>亚 5% 聚合行的单数写法。</summary>
        private const string OtherFiber = "other fiber";

        /// <summary>亚 5% 聚合行的复数写法（聚合了 2 项及以上时用，见 <see cref="CalculateFormattedResults"/> 第 1.5 步）。</summary>
        private const string OtherFibers = "other fibers";

        /// <summary>
        /// 通用计算方法：提取成分、四舍五入、调整最大项、格式化
        /// </summary>
        /// <param name="calculatedFiberResult">计算结果</param>
        /// <param name="decimalPlaces">保留小数位（0=整数, 1=1位小数）</param>
        /// <param name="format">格式化字符串（F0 或 F1）</param>
        private List<string> CalculateFormattedResults(List<CalculatedFiberResult> calculatedFiberResult, int decimalPlaces, string format, bool isAatcc = false)
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

            // 1.5 AATCC 美标（16 CFR § 303.3(a)）：原始 Rate < 5% 的纤维**不得写通用名**，一律并成一行。
            //     两类豁免见 IsNamedBelowFivePercent。
            if (isAatcc)
            {
                var normal = new List<(string Name, decimal Rate)>();
                decimal otherSum = 0m;
                int otherCount = 0;

                foreach (var c in rawComponents)
                {
                    if (c.Rate < 5m && !IsNamedBelowFivePercent(c.Name))
                    {
                        otherSum += c.Rate;
                        otherCount++;
                    }
                    else
                    {
                        normal.Add(c);
                    }
                }

                // 法规原文：只有 1 个 → "other fiber"；多个 → **聚合**为 "other fibers"。
                // 判据用 otherCount（聚合了几项）而不是 otherSum（合计百分比）—— 单复数是"几项"的问题。
                // 外层的 otherSum > 0 守卫沿用改动前：全是 0% 时不产生这一行。
                if (otherSum > 0)
                    normal.Add((otherCount > 1 ? OtherFibers : OtherFiber, otherSum));

                rawComponents = normal;
            }

            // 2. 四舍五入
            var rounded = rawComponents
                .Select(c => new
                {
                    c.Name,
                    RoundedRate = Math.Round(c.Rate, decimalPlaces, MidpointRounding.AwayFromZero)
                })
                .ToList();

            // 2.5 取整后 0% → 1%，最大项同步扣减
            for (int i = 0; i < rounded.Count; i++)
            {
                if (rounded[i].RoundedRate == 0m)
                {
                    rounded[i] = new { rounded[i].Name, RoundedRate = 1m };
                    var maxIdx = 0;
                    for (int j = 1; j < rounded.Count; j++)
                        if (rounded[j].RoundedRate > rounded[maxIdx].RoundedRate) maxIdx = j;
                    rounded[maxIdx] = new { rounded[maxIdx].Name,
                        RoundedRate = rounded[maxIdx].RoundedRate - 1m };
                }
            }

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

            // 5. 格式化输出（亚 5% 的聚合行排**最后**，其余按 Rate 从大到小排序）
            //    16 CFR § 303.16(a)(1)（羊毛制品见 § 300.3(b)）：通用名按占比由多到少排列，
            //    "other fiber" / "other fibers" 必须出现在末尾。
            return rounded
                .OrderBy(r => IsOtherFiberLine(r.Name) ? 1 : 0)
                .ThenByDescending(r => r.RoundedRate)
                .Select(r => $"{r.RoundedRate.ToString(format)}% {r.Name}")
                .ToList();
        }

        /// <summary>
        /// 亚 5% 却**仍然点名**的两类纤维 —— 16 CFR § 303.3(a) 给了两个豁免，这里各用其一：
        ///
        /// · **功能性**：原文举的例子就是 4% spandex（"96 percent acetate, 4 percent spandex"）。
        ///   代码只认这一例（<see cref="FiberTokens.IsElastane"/>），不外推。原文另一例是
        ///   2% nylon，但"某项小比例纤维有没有明确功能"要实验室逐单判，程序替它断言不如老实合并。
        /// · **羊毛**：§ 300.3(b)（羊毛法）—— 羊毛/回收羊毛**无论多少都得点名带百分比**。
        ///   名单见 <see cref="FiberTokens.IsWoolFamily"/>，比"动物纤维"窄得多。
        ///
        /// 只管**点不点名**，不管排序 —— 排序见 <see cref="CalculateFormattedResults"/> 第 5 步。
        /// </summary>
        private static bool IsNamedBelowFivePercent(string name)
            => FiberTokens.IsElastane(name) || FiberTokens.IsWoolFamily(name);

        /// <summary>是不是亚 5% 的聚合行 —— § 303.16(a)(1) 要求它排在最后，两种写法都算。</summary>
        private static bool IsOtherFiberLine(string name)
            => name.Equals(OtherFiber, StringComparison.OrdinalIgnoreCase)
            || name.Equals(OtherFibers, StringComparison.OrdinalIgnoreCase);

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
                        foreach (var r in multi.MultiFiberRowUnits)
                        {
                            if (string.IsNullOrWhiteSpace(r.Sum) || r.Sum.Contains('/'))
                                continue;  // 排除缩写拼接的起始行（如 E/T）

                            // cellulosic fibre 展开为子纤维
                            if ((r.Sum == "*cellulosic fibre" || r.Sum == "*Regenerated cellulose fibre") && r.CellulosicSubFibers.Count > 0)
                            {
                                foreach (var sub in r.CellulosicSubFibers)
                                {
                                    if (!string.IsNullOrWhiteSpace(sub.FiberName) && sub.Percentage > 0)
                                        components.Add((sub.FiberName, r.Rate * sub.Percentage / 100m));
                                }
                            }
                            else if (IsBicomponentFiber(r.Sum) && r.BicomponentSubFibers.Count > 0)
                            {
                                // bicomponent：只加父行，子纤维由 PostProcessBicomponent 处理
                                components.Add((r.Sum, r.Rate));
                            }
                            else
                            {
                                components.Add((r.Sum, r.Rate));
                            }
                        }
                        break;
                }
            }

            // 合并同名纤维（拆分和溶解中有相同纤维时百分比相加）
            components = components
                .GroupBy(c => c.Name)
                .Select(g => (g.Key, g.Sum(c => c.Rate)))
                .ToList();

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
            // 空名守卫。下面 `_ => fiberName[..1]` 对空串会抛 ArgumentOutOfRangeException，
            // 而这个方法是**在 Calculate 内部**被调用的（溶解组缩写拼接）——一抛就是整条记录算不出来。
            // 空名另由 adapter 的空行跳过挡在更外层，这里再兜一层：
            // 两条防线各自独立，谁先生效都不影响另一条。
            if (string.IsNullOrWhiteSpace(fiberName))
                return string.Empty;

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
                    .Select(c => c.FiberName));
        }

        /// <summary>
        /// 多组分：从 _components 中提取所有纤维名称。cellulosic fibre 展开为子纤维名。
        /// </summary>
        private string GetMultipleFiberNames()
        {
            var dissolvedNames = _components
                .OfType<DissolvedFiberComponent>()
                .SelectMany(d => d.DissolutionUnits)
                .SelectMany(u => IsBicomponentFiber(u.FiberName) && u.BicomponentSubFibers.Count > 0
                    ? u.BicomponentSubFibers.Where(b => !string.IsNullOrWhiteSpace(b.FiberName)).Select(b => b.FiberName)
                    : (u.FiberName == "*cellulosic fibre" || u.FiberName == "*Regenerated cellulose fibre") && u.CellulosicSubFibers.Count > 0
                        ? u.CellulosicSubFibers.Where(s => !string.IsNullOrWhiteSpace(s.FiberName)).Select(s => s.FiberName)
                        : new[] { u.FiberName })
                .Distinct();

            var splittingNames = _components
                .OfType<SplittingFiberComponent>()
                .SelectMany(s => IsBicomponentFiber(s.FiberName) && s.BicomponentSubFibers.Count > 0
                    ? s.BicomponentSubFibers.Where(b => !string.IsNullOrWhiteSpace(b.FiberName)).Select(b => b.FiberName)
                    : (s.FiberName == "*cellulosic fibre" || s.FiberName == "*Regenerated cellulose fibre") && s.CellulosicSubFibers.Count > 0
                        ? s.CellulosicSubFibers.Where(c => !string.IsNullOrWhiteSpace(c.FiberName)).Select(c => c.FiberName)
                        : new[] { s.FiberName })
                .Distinct();

            return string.Join("/", dissolvedNames.Concat(splittingNames));
        }

        private static bool IsBicomponentFiber(string name) =>
            name is "Bicomponent Fiber" or "Biconstituent Fiber";

        /// <summary>
        /// Bicomponent/Biconstituent 格式化：
        /// TestResult: "X% Polyester/Polyamide bicomponent (Y%Polyester Z%Polyamide)"
        /// Recommendation: "X% Bicomponent Fiber (Y%Polyester Z%Polyamide)"
        /// </summary>
        private List<string> PostProcessBicomponent(List<string> results,
            List<CalculatedFiberResult> calculatedFiberResult, string format)
        {
            var multi = calculatedFiberResult.OfType<MultiCalculatedFiberItem>().FirstOrDefault();
            if (multi?.MultiFiberRowUnits == null) return results;

            for (int i = 0; i < results.Count; i++)
            {
                var line = results[i];
                if (!line.Contains("Bicomponent Fiber") && !line.Contains("Biconstituent Fiber")) continue;

                var row = multi.MultiFiberRowUnits
                    .FirstOrDefault(r => IsBicomponentFiber(r.Sum));
                if (row == null || row.BicomponentSubFibers.Count != 2) continue;

                var subs = row.BicomponentSubFibers;
                var subGsmTotal = subs.Sum(s => s.GSMTrail1 + s.GSMTrail2);
                if (subGsmTotal == 0) continue;

                var parentPct = line.Split('%')[0].Trim();
                var fullName = line.Contains("Bicomponent Fiber")
                    ? "Bicomponent Fiber" : "Biconstituent Fiber";
                var shortName = line.Contains("Bicomponent Fiber")
                    ? "bicomponent" : "biconstituent";

                var p1 = subs[0];
                var p2 = subs[1];
                var s1Name = p1.FiberName;
                var s2Name = p2.FiberName;
                var s1Gsm = p1.GSMTrail1 + p1.GSMTrail2;
                var s2Gsm = p2.GSMTrail1 + p2.GSMTrail2;
                var mr1 = LookupMoistureRegain(s1Name);
                var mr2 = LookupMoistureRegain(s2Name);
                var correctedS1 = s1Gsm * (1 + mr1 / 100m);
                var correctedS2 = s2Gsm * (1 + mr2 / 100m);
                var denominator = correctedS1;
                if (denominator == 0) continue;

                string s1Pct, s2Pct;
                if (format == "F0")
                {
                    var s2Raw = (correctedS2 / denominator) * 100m;
                    var s2Rounded = Math.Round(s2Raw, MidpointRounding.AwayFromZero);
                    s2Pct = s2Rounded.ToString("F0");
                    s1Pct = (100m - s2Rounded).ToString("F0");
                }
                else
                {
                    s2Pct = ((correctedS2 / denominator) * 100m).ToString("F1");
                    s1Pct = ((1m - correctedS2 / denominator) * 100m).ToString("F1");
                }

                if (line.Contains("Biconstituent Fiber"))
                    results[i] = $"{parentPct}% {fullName} ({s1Pct}%{s1Name} {s2Pct}%{s2Name})";
                else
                    results[i] = $"{parentPct}% {s1Name}/{s2Name} {shortName} ({s1Pct}%{s1Name} {s2Pct}%{s2Name})";
            }

            return results;
        }

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
        // 显微镜设备串统一为 `Microscope: SFL-NGB-EQP-XXX`（冒号后带空格 + 设备号用连字符）。
        // 原先这两处格式不一致（一处无空格、一处用下划线）。纯报告外观一致性，不涉及模板匹配
        // —— 模板与已生成 docx 里 `SFL` 均 0 命中，设备串是整串写进 Equipment 书签。
        private const string MICROSCOPE = "Microscope: SFL-NGB-EQP-056";
        private const string MICROSCOPE_CELLULOSIC = "Microscope: SFL-NGB-EQP-268";
        private const string OVEN = "Oven:SFL-NGB-EQP-164";
        private const string BALANCE = "Balance:SFL-NGB-EQP-061";
        private const string WATER_BATH = "Water bath:SFL-NGB-EQP-046";
        private const string SHAKER = "Shaker:SFL-NGB-EQP-052";

        // Shaker 触发纤维（首成分为这些时需化学溶解）
        private static readonly HashSet<string> ShakerFirstFibers = new(StringComparer.OrdinalIgnoreCase)
        {
            "nylon", "polyamide", "wool"
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

            // P131: 任何相邻对中前者为丙烯腈类且后者存在 → 水浴（回退）
            // 这里原先是第三处裸字面量 `f == "acrylic"`，与 ISO -12 / GB .12 两处本属同一语义。
            // 不收编的话，Modacrylic 的方法栏会印 DMF 法（该分部试剂就是 DMF、需要水浴），
            // 设备栏却不给水浴 —— 方法栏与设备栏自相矛盾。三处一起改。
            for (int i = 1; i < fibers.Count; i++)
            {
                if (FiberTokens.IsAcrylicType(fibers[i - 1])
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

            // 首成分为 nylon/polyamide/wool 且有 ≥2 成分 → Shaker
            if (ShakerFirstFibers.Contains(fibers[0]))
                return SHAKER;

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

        /// <summary>
        /// 槽位通道。**名字与顺序的唯一真源仍是 <see cref="GetOrderedFiberNames"/>**，
        /// 这里只按同样的序遍历第二遍、把每个槽的 CellulosicSubFibers 取出来。
        ///
        /// 这样切分是为了把分歧面压到最小：名字和顺序不可能是"两份各自算的"，
        /// 只可能是"子纤维挂错了槽"。长度对不上会**抛**（见下面的守卫），不会静默错配
        /// —— 错配的表现是亚麻配到了别的纤维上，报告上看不出来。
        ///
        /// **只展开 cellulosic 子纤维，不展开 bicomponent 子纤维**（刻意，别顺手加）。
        /// Bicomponent Fiber 在报告上是**一个成分行**、百分比在行内拆
        /// （见 PostProcessBicomponent）：它的两个子纤维是同一根物理纤维的两个组成部分，
        /// 不是两个并列成分。展开会让成分数从 1 变 2，凭空翻掉一批记录的三元法分支，
        /// 而槽位通道的实测动机（亚麻 18 条）与它无关。
        ///
        /// 展开还有一层前提：**父槽必须是分组父槽**（见 <see cref="BuildSlot"/>）。
        /// </summary>
        private List<FiberSlot> GetOrderedFiberSlots()
        {
            var names = GetOrderedFiberNames();

            // 单组分没有拆分/溶解列，也就没有子纤维
            if (Type == AnalysisType.Single)
                return names.Select(FiberSlot.Leaf).ToList();

            var slots = new List<FiberSlot>();

            // 拆分列先、溶解列后 —— 与 GetOrderedFiberNames 用**同一组 OrderBy 键**
            foreach (var s in Components.OfType<SplittingFiberComponent>().OrderBy(s => s.SplittingOrder))
            {
                slots.Add(BuildSlot(s.FiberName, s.CellulosicSubFibers));
            }

            foreach (var d in Components.OfType<DissolvedFiberComponent>())
            {
                foreach (var unit in d.DissolutionUnits.OrderBy(u => u.DissolutionStep))
                {
                    slots.Add(BuildSlot(unit.FiberName, unit.CellulosicSubFibers));
                }
            }

            // 守卫：两个序列同源同长。不等只可能是有人改了 GetOrderedFiberNames 的遍历而没同步这里
            // —— 直接抛，不要带着错位的子纤维继续算。
            if (slots.Count != names.Count)
            {
                throw new InvalidOperationException(
                    $"槽位通道与 GetOrderedFiberNames() 长度不一致（{slots.Count} vs {names.Count}）：" +
                    "两处遍历必须同步维护，见 GetOrderedFiberSlots 的注释。");
            }

            return slots;
        }

        /// <summary>
        /// 子纤维名取全的槽；没有（或全是空白）就退化成叶子槽。
        ///
        /// 空白过滤与 <see cref="GetMultipleFiberNames"/> 同一口径（前端会提交空行）。
        /// 这里**不做 Trim** —— 规则表的谓词自带 Trim()，多一层反而让"配对用的名字"
        /// 与"报告上印的名字"不一致。
        ///
        /// **只有分组父槽才展开**（计数口径：父槽不单列、被其子纤维 1 换 N）。
        /// 别的槽挂子纤维是录入错位 —— 实测 87.405.26.46546.01 把 cotton/linen
        /// 挂在 Polyamide 行上，展开它等于凭空造出两个成分、把成分数抬到 3 触发三元法早退，
        /// 挤掉真实的 ISO1833-7:2017。修法是**不展开** —— 这条记录的输出就此回到与展开前逐字同值。
        ///
        /// **刻意不抛异常**：那条记录今天算得出来、报告能出；抛出去会让它在生产里直接报错，
        /// 而它的问题（子纤维挂错槽）该由前端的录入侧去修，不该在这里拦住整张单。
        /// </summary>
        private static FiberSlot BuildSlot(string name, List<CellulosicSubFiber> subFibers)
        {
            if (!FiberTokens.IsCellulosicGroupParent(name))
                return FiberSlot.Leaf(name);

            var subs = subFibers
                .Where(x => !string.IsNullOrWhiteSpace(x.FiberName))
                .Select(x => x.FiberName)
                .ToList();

            return subs.Count > 0 ? new FiberSlot(name, subs) : FiberSlot.Leaf(name);
        }

        /*------------------------------------------计算逻辑------------------------------------------------------------------------------*/
    }
}