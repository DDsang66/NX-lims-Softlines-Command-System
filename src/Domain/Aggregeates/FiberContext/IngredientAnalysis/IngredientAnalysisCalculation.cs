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
        /// 纤维英文名 → 中文名。**与标准无关**，一份实例的各段共用同一张表。
        /// 表本身的取舍（读哪张库表、空值怎么丢）见 <c>IFiberDatabaseRepository.GetChineseNameMapAsync</c>。
        /// </summary>
        private IReadOnlyDictionary<string, string> _fiberChineseNames = EmptyChineseNames;

        private static readonly IReadOnlyDictionary<string, string> EmptyChineseNames
            = new Dictionary<string, string>();

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
        public AnalysisResult Calculate(
            IReadOnlyDictionary<string, decimal>? moistureRegainMap = null,
            IReadOnlyDictionary<string, string>? fiberChineseNames = null)
        {
            Result = CalculateForStandard(SelectedStandard, moistureRegainMap, fiberChineseNames);
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
            IReadOnlyDictionary<string, IReadOnlyDictionary<string, decimal>> mrByStandard,
            IReadOnlyDictionary<string, string>? fiberChineseNames = null)
        {
            var list = StandardsToRender
                .Select(std => CalculateForStandard(
                    std,
                    mrByStandard != null && mrByStandard.TryGetValue(std, out var m) ? m : null,
                    fiberChineseNames))
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
            IReadOnlyDictionary<string, decimal>? moistureRegainMap,
            IReadOnlyDictionary<string, string>? fiberChineseNames = null)
        {
            _moistureRegainMap = moistureRegainMap ?? new Dictionary<string, decimal>();
            _fiberChineseNames = fiberChineseNames ?? EmptyChineseNames;

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
            // 闸门是**分析类型**，不是纤维条数 —— 见 SelectEquipment 的说明。
            // 显微镜**只有一台**，父槽记录与普通记录同口径：原先"含 cellulosic 父槽就追加。
            var orderedFiberNames = GetOrderedFiberNames();
            var equipment = SelectEquipment(Type, orderedFiberNames);
            result = result.WithEquipment(equipment);

            // 3.6) 自动拼接 Methods（对应 Excel L4 公式）
            // 配对另走**槽位通道**（第三条通道），GetOrderedFiberNames() 一字不动 —— 它继续喂
            // 上面的设备选型、上面的显微镜追加、以及规则表里的三处 `*cellulosic fibre` 字面量判定。
            // 三条通道职责不同，不合并：扁平列表管"报告上印什么"，槽位管"拿哪些名字去查表"。
            //
            // 末位那个 `Type == Single` 是**整条链的分流开关**：单组分走
            // BuildSingleComponentChain —— 一律不派生定量子标准，只出勾选的标准本身 + 鉴别法。
            // 详见 BuildMethodString 的 isSingleComponent 说明。
            // ⚠️ **不能**按纤维条数代传。
            var methodString = FiberStandardChainBuilder.BuildMethodString(
                standard, orderedFiberNames, GetOrderedFiberSlots(), Type == AnalysisType.Single);
            result = result.WithMethods(methodString);

            // 3.6.1) 国标中文化 —— **按段**判定，读的是本份的标准（与上面那条同源）。
            // 闸门只有这一个出处：报告侧的五个标签、Test Result/Recommendation 的纤维名、
            // 页脚 MR 汇总，全都从 UseChineseNames 这一个布尔分流，不各判各的。
            result = result.WithChineseNames(_fiberChineseNames, FiberChineseName.IsChineseReport(standard));

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
                // 双组分父行的总重取**父行自己的称量值**。
                // 原先取的是子行一（"父 GSM 取子行第一个的数据"），于是"父重 = 两子之和"那
                // 11 条记录的父行被算成了其中一份，外层百分比偏小。见 ResolveBicomponentParentGsm。
                var (actualGsm1, actualGsm2) = ResolveBicomponentParentGsm(
                    s.GSMTrail1, s.GSMTrail2, s.BicomponentSubFibers);

                var rateTrail1 = totalGSMTrail1 == 0 ? 0 : actualGsm1 / totalGSMTrail1 * 100;
                var rateTrail2 = totalGSMTrail2 == 0 ? 0 : actualGsm2 / totalGSMTrail2 * 100;
                var avg = (rateTrail1 + rateTrail2) / TrialDivisor(actualGsm1, actualGsm2);

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
                        Avg = (SafeDivide(startGsm1, totalGSMTrail1) + SafeDivide(startGsm2, totalGSMTrail2))
                              / TrialDivisor(startGsm1, startGsm2),
                        // 组头行不是纤维，没有损伤系数 —— 与下面 MoistureRegain 留空同源，
                        // 适配器据此印空白。原工作簿的组头行也没有 Correct 这一格。
                        // 该行被 allComponentRows 的两道 Where 挡在 Rate 分母外，故这里填 0 不动任何算术。
                        Correct = 0,
                        MoistureRegain = 0,
                        Rate = 0
                    });
                }

                for (int i = 0; i < componentCount; i++)
                {
                    var current = groupUnits[i];
                    var isLast = i == componentCount - 1;

                    decimal ownGsm1, ownGsm2, curGsm1, curGsm2;
                    // 同 CalculateSplittingUnits —— 双组分父行取父行自己的称量值。
                    (curGsm1, curGsm2) = ResolveBicomponentParentGsm(
                        current.GSMTrail1, current.GSMTrail2, current.BicomponentSubFibers);

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
                        Avg = (rateTrail1 + rateTrail2) / TrialDivisor(curGsm1, curGsm2),
                        // 损伤系数挂在"前一个成分被溶掉、本行是残留"这一对上，所以组内第一个成员恒 1。
                        // 见 FiberDamageFactor 的类注释。
                        Correct = i == 0 ? 1m : FiberDamageFactor.Resolve(groupUnits[i - 1].FiberName, current.FiberName),
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
                    // 从 MoistureRegainMap 查回潮率（双组分父行走 EffectiveMoistureRegain）
                    var mr = EffectiveMoistureRegain(r);
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

                    // 查回潮率：**算**用 Effective，**印**用 Displayed —— 双组分父行两者刻意不同
                    // （2026-09-28 用户裁定：那个加权回潮率只用来算，不上报告）。
                    var mr = EffectiveMoistureRegain(row);

                    // 计算分子
                    var numerator = (1m + mr / 100m) * row.Correct / 100m * row.Avg;

                    // 计算Rate（不取整，保留全精度，最终格式化时统一取整）
                    var rate = numerator / denominator * 100m;

                    return row with
                    {
                        MoistureRegain = DisplayedMoistureRegain(row),
                        // "印不印"与"值是多少"是两件事：真值 0 要印 0.00%，查不到才留空。
                        MoistureRegainKnown = IsMoistureRegainKnown(row),
                        Rate = rate
                    };

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

            // 国标（FZ/T 01057）：结果行里的纤维名印成 `中文English`。
            // 判据同样读**入参**、不读 Methods，理由与上面 isAatcc 一字不差。
            // 与 AATCC 天然互斥 —— 一个标准串不可能既是 FZ/T 又是 AATCC。
            var isGb = FiberChineseName.IsChineseReport(standard);

            var result = new CalculatedRemarkResult {
                RecommendedLabel = new List<string>(remarkLabel.RecommendedLabel),
                ResultRemark = remarkLabel.ResultRemark,
                LabelRemark = remarkLabel.LabelRemark,
                JudgmentLabelRemark = remarkLabel.JudgmentLabelRemark,
                LanguageLabelRemark = remarkLabel.LanguageLabelRemark,
                VerifyResult = remarkLabel.VerifyResult,
                Results = CalculateFormattedResults(calculatedFiberResult, 1, "F1", isAatcc, isGb),
                Recommendation = CalculateFormattedResults(calculatedFiberResult, 0, "F0", isAatcc, isGb)
            };

            // Bicomponent/Biconstituent 格式化后处理
            result = result with
            {
                Results = PostProcessBicomponent(result.Results, calculatedFiberResult, "F1"),
                Recommendation = PostProcessBicomponent(result.Recommendation, calculatedFiberResult, "F0")
            };

            // 最后一步：百分比右对齐补齐，让两列的名称落在同一竖列。
            // 位置是硬的 —— 必须在 PostProcessBicomponent **之后**，理由见 AlignResultLines。
            result = result with
            {
                Results = AlignResultLines(result.Results),
                Recommendation = AlignResultLines(result.Recommendation)
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
        /// <param name="isAatcc">本份是否 AATCC（亚 5% 聚合行）。</param>
        /// <param name="useChineseNames">
        /// 本份是否国标 —— 纤维名印成 `中文English`。
        /// **只加在名字位置**（第一个 `%` 之后），所以下游两处解析都取不到它：
        /// <see cref="AlignResultLines"/> 只认 `%` 之前，<see cref="PostProcessBicomponent"/> 整行重写。
        /// 双组分父行因此天然保持英文，无需在这里特判。
        /// </param>
        private List<string> CalculateFormattedResults(List<CalculatedFiberResult> calculatedFiberResult, int decimalPlaces, string format, bool isAatcc = false, bool useChineseNames = false)
        {
            // 单组分：每个纤维固定 100%，不求和。
            // 单组分**不走** AlignResultLines 的对齐：LeadingPercentNumber 见到 `{Sample}:`
            // 里的冒号就返回 null，这行永远不参与补齐 —— 去掉 `\n` 前后都是这样。
            if (calculatedFiberResult.All(c => c is SingleCalculatedFiberItem))
            {
                return calculatedFiberResult
                    .OfType<SingleCalculatedFiberItem>()
                    .Select(s => $"{s.Sample}:  100% {ResultFiberName(s.FiberName, useChineseNames)}")
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
                    // 原始值一路带着走：**排序键是它，不是 RoundedRate**（理由见第 5 步）。
                    c.Rate,
                    RoundedRate = Math.Round(c.Rate, decimalPlaces, MidpointRounding.AwayFromZero)
                })
                .ToList();

            // 2.5 取整后 0% → 1%，最大项同步扣减
            for (int i = 0; i < rounded.Count; i++)
            {
                if (rounded[i].RoundedRate == 0m)
                {
                    rounded[i] = new { rounded[i].Name, rounded[i].Rate, RoundedRate = 1m };
                    var maxIdx = 0;
                    for (int j = 1; j < rounded.Count; j++)
                        if (rounded[j].RoundedRate > rounded[maxIdx].RoundedRate) maxIdx = j;
                    rounded[maxIdx] = new { rounded[maxIdx].Name, rounded[maxIdx].Rate,
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
                            rounded[i].Rate,
                            RoundedRate = rounded[i].RoundedRate + diff  // 加或减差值
                        };
                        break;
                    }
                }
            }

            // 5. 格式化输出（亚 5% 的聚合行排**最后**，其余按 Rate 从大到小排序）
            //    16 CFR § 303.16(a)(1)（羊毛制品见 § 300.3(b)）：通用名按占比由多到少排列，
            //    "other fiber" / "other fibers" 必须出现在末尾。
            //
            //  ⚠️ 排序键必须是**原始 Rate，不能是 RoundedRate**（2026-09-29 用户裁定）：
            //  本方法被同一个记录调两次，取位不同（Test Result 1 位 / Recommendation 0 位），
            //  用取整值排就会出现"一边还分得出大小、另一边已经全平"的情况 ——
            //  全平那边退化成输入顺序，于是两列顺序对不上。实测 87.405.26.61074.01：
            //  Wool 0.57 / Polyamide 1.20 / Acrylic 1.19 → 1 位是 0.6/1.2/1.2（排得出序），
            //  0 位是 1/1/1（全平，退成表里的行序 Wool→Polyamide→Acrylic）。
            //  改用原始值后两列的输入与键都相同，顺序必然一致；
            //  只有原始值**恰好相等**时才退到输入顺序 —— 那时两列仍然是同一个顺序。
            return rounded
                .OrderBy(r => IsOtherFiberLine(r.Name) ? 1 : 0)
                .ThenByDescending(r => r.Rate)
                .Select(r => $"{r.RoundedRate.ToString(format)}% {ResultFiberName(r.Name, useChineseNames)}")
                .ToList();
        }

        /// <summary>
        /// 报告上印的纤维名：国标且查得到中文 → `中文English`，否则原样。
        ///
        /// 这里是**唯一的**取名口，单组分与多组分两条格式化分支都走它 ——
        /// 排序、去重、亚 5% 聚合一律仍用英文原名做键，中文化只发生在最后拼串那一步。
        /// </summary>
        private string ResultFiberName(string name, bool useChineseNames)
            => useChineseNames ? FiberChineseName.Localize(name, _fiberChineseNames) : name;

        /// <summary>
        /// 把 `<数字>% <名称>` 这类结果行按**本块内最长的数字**补空格，让名称落在同一竖列。
        ///
        /// **每缺一位补两个空格，不是补一个** —— 结果行的字体是 Arial（比例字体）：
        /// 数字宽 0.556em、空格只有 0.278em，**恰好一半**。补一个空格只把名称推半格，
        /// 看着就是"好像没对齐"。
        /// ⚠️ **必须排在 <see cref="PostProcessBicomponent"/> 之后**：双组分那条会**整行重写**
        /// （父行百分比取的是 `line.Split('%')[0].Trim()`），`Trim()` 会把先补的空格吃掉，先补等于白补。
        ///
        /// 认不出的行原样返回 —— 单组分那种 `Sample:  100% Name`（全是 100%，补了也是空操作）走这条路。
        /// </summary>
        private static List<string> AlignResultLines(List<string> lines)
        {
            int width = 0;
            foreach (var line in lines)
            {
                if (LeadingPercentNumber(line) is { } n)
                    width = Math.Max(width, n.Length);
            }

            if (width == 0) return lines;

            var aligned = new List<string>(lines.Count);
            foreach (var line in lines)
            {
                var n = LeadingPercentNumber(line);
                // 前置补空格，每缺一位补两个 —— Arial 下 2 个空格才等于 1 个数字宽（见方法注释）。
                aligned.Add(n is null ? line : line.PadLeft(line.Length + (width - n.Length) * 2));
            }
            return aligned;
        }

        /// <summary>
        /// 取 `<数字>% ...` 里那个数字串；不是这个形状就返回 null（该行不参与对齐）。
        /// 只认**第一个** `%` 之前的内容 —— 双组分行后面括号里的 `16.4%Polyamide` 因此不会被误取。
        /// </summary>
        private static string? LeadingPercentNumber(string line)
        {
            // 带换行的行一律不参与对齐：多行值补行首空格会补进第一行里，越补越乱。
            // 现在**已经没有这种行了**（单组分原先那条 `{Sample}:\n100% {Name}` 已改单行），
            // 留着是防日后又出多行格式被静默补坏 —— 别当死代码删。
            if (line.Contains('\n')) return null;

            var pct = line.IndexOf('%');
            if (pct <= 0) return null;

            // 单组分 `{Sample}:  100% {Name}` 走这里会因为冒号（或样品号里的字母）落进上面那条 all-digit 判定 → null。
            var head = line[..pct];
            return head.All(ch => char.IsAsciiDigit(ch) || ch == '.') ? head : null;
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

        /// <summary>按名字查回潮率，**查不到折成 0** —— 算术路径要的就是这个，别改。</summary>
        private decimal LookupMoistureRegain(string fiberName) => FindMoistureRegain(fiberName) ?? 0m;

        /// <summary>
        /// 按名字查回潮率；**查不到返回 null**。
        ///
        /// 与 <see cref="LookupMoistureRegain"/> 唯一的区别就是"查不到"这个返回值：
        /// 那个折成 0 喂算术，这个留着 null，好让报告分得清
        /// 「表里就是 0」和「没有这个数」（见 <see cref="IsMoistureRegainKnown"/>）。
        /// </summary>
        private decimal? FindMoistureRegain(string fiberName)
        {
            if (string.IsNullOrWhiteSpace(fiberName) || _moistureRegainMap.Count == 0)
                return null;

            if (_moistureRegainMap.TryGetValue(fiberName, out var exact))
                return exact;

            // 大小写不敏感兜底。用显式循环而不是 FirstOrDefault ——
            // 后者没匹配上时返回 default，Key 是 null，得靠"字典不允许 null 键"来判，太绕。
            foreach (var kv in _moistureRegainMap)
            {
                if (string.Equals(kv.Key, fiberName, StringComparison.OrdinalIgnoreCase))
                    return kv.Value;
            }
            return null;
        }

        /// <summary>
        /// 这个成分行的回潮率**该不该印到报告上**。
        ///
        /// 两道闸，缺一不可：
        /// ① 纤维表里真有这个数 —— **真值 0 也算有**，这正是本次要修的（2026-09-29 用户裁定）；
        /// ② 不是双组分父行 —— 父行那个加权值只用来算，不上报告（2026-09-28 裁定）。
        ///
        /// 两道都不满足时 MR 栏留空。组头行（`Sum` 是 `M/E/S` 这种缩写串）走①就被挡下了。
        /// </summary>
        private bool IsMoistureRegainKnown(MultiFiberRowUnit row)
            => !IsBicomponentFiber(row.Sum) && FindMoistureRegain(row.Sum) is not null;

        /// <summary>
        /// 某个成分行**用来算**的回潮率。
        ///
        /// 双组分父行不能按名字查：`Bicomponent Fiber` / `Biconstituent Fiber` 这两行**不在
        /// `fiber_database` 里**（实测精确命中 0 行），查出来是 0 —— 等于"父行不回潮"，会把
        /// 父行的百分比压低。正确做法是把它按两个子成分拆开、各按自己的回潮率折干后相加，
        /// 再折成一个**加权回潮率**：干重/湿重 − 1（见 <see cref="DecomposeBicomponent"/>）。
        ///
        /// 非双组分行、子行不足两条、或拆不出质量时，一律退回按名字查表 —— 逐字是改动前的行为。
        /// </summary>
        private decimal EffectiveMoistureRegain(MultiFiberRowUnit row)
        {
            if (!IsBicomponentFiber(row.Sum) || row.BicomponentSubFibers.Count < 2)
                return LookupMoistureRegain(row.Sum);

            var parentWet = row.GSMTrail1 + row.GSMTrail2;
            var (residueDry, dissolvedDry) = DecomposeBicomponent(
                row.GSMTrail1, row.GSMTrail2, row.BicomponentSubFibers);
            if (parentWet == 0 || residueDry + dissolvedDry == 0)
                return LookupMoistureRegain(row.Sum);

            return (residueDry + dissolvedDry) / parentWet * 100m - 100m;
        }

        /// <summary>
        /// 某个成分行**印到报告上**的回潮率（MR 栏 / 页脚汇总）。
        ///
        /// 双组分父行恒为 0，**刻意与 <see cref="EffectiveMoistureRegain"/> 不同** ——
        /// 那个加权值只用来算，不上报告（MR 栏保持空白、页脚不多一条）。
        /// 别为了"一致"把这里改回 Effective：要让报告显示它，是产品决定，不是修 bug。
        /// </summary>
        private decimal DisplayedMoistureRegain(MultiFiberRowUnit row)
            => IsBicomponentFiber(row.Sum) ? 0m : EffectiveMoistureRegain(row);

        /// <summary>
        /// 安全除法：除数为0时返回0，避免异常
        /// </summary>
        private static decimal SafeDivide(decimal numerator, decimal denominator)
        {
            return denominator == 0 ? 0 : numerator / denominator * 100;  // 返回百分比
        }

        /// <summary>
        /// Average 列的**除数** —— 这一行实际有几组试次（Trial#2 的称量 > 0 才算有第二组）。
        ///
        /// 原先三处恒 `/ 2`，于是"只称了一次"的行 Average 印出来正好是 Trial#1 的一半
        ///
        /// ⚠️ 会连带改变最终 Rate：Avg 同时是 Rate 的分子与分母（见 <see cref="CalculateRates"/>），
        /// 全体行同系数时约掉，只有除数不齐的记录会动。这是应有结果，不是副作用。
        /// </summary>
        private static decimal TrialDivisor(decimal gsmTrail1, decimal gsmTrail2)
            => gsmTrail1 > 0 && gsmTrail2 > 0 ? 2m : 1m;

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
        /// <remarks>
        /// 定性名是一根纤维一次。去重必须放在两列合并之后。
        ///
        /// 比较器是 ordinal（<c>Distinct</c> 默认），与正文 <c>ExtractComponents</c> 的
        /// <c>GroupBy(Name)</c>、页脚 MR 汇总的 <c>DistinctBy</c> 同一套 —— **别改成大小写折叠**：
        /// 单这一处折叠会让"定性列把两条并了、正文却没并"。
        ///
        /// 顺序保持不变：溶解列在前、拆分列在后，去重保留首现。
        /// </remarks>
        private string GetMultipleFiberNames()
        {
            var dissolvedNames = _components
                .OfType<DissolvedFiberComponent>()
                .SelectMany(d => d.DissolutionUnits)
                .SelectMany(u => IsBicomponentFiber(u.FiberName) && u.BicomponentSubFibers.Count > 0
                    ? u.BicomponentSubFibers.Where(b => !string.IsNullOrWhiteSpace(b.FiberName)).Select(b => b.FiberName)
                    : (u.FiberName == "*cellulosic fibre" || u.FiberName == "*Regenerated cellulose fibre") && u.CellulosicSubFibers.Count > 0
                        ? u.CellulosicSubFibers.Where(s => !string.IsNullOrWhiteSpace(s.FiberName)).Select(s => s.FiberName)
                        : new[] { u.FiberName });

            var splittingNames = _components
                .OfType<SplittingFiberComponent>()
                .SelectMany(s => IsBicomponentFiber(s.FiberName) && s.BicomponentSubFibers.Count > 0
                    ? s.BicomponentSubFibers.Where(b => !string.IsNullOrWhiteSpace(b.FiberName)).Select(b => b.FiberName)
                    : (s.FiberName == "*cellulosic fibre" || s.FiberName == "*Regenerated cellulose fibre") && s.CellulosicSubFibers.Count > 0
                        ? s.CellulosicSubFibers.Where(c => !string.IsNullOrWhiteSpace(c.FiberName)).Select(c => c.FiberName)
                        : new[] { s.FiberName });

            return string.Join("/", dissolvedNames.Concat(splittingNames).Distinct());
        }

        private static bool IsBicomponentFiber(string name) =>
            name is "Bicomponent Fiber" or "Biconstituent Fiber";

        /// <summary>
        /// 双组分父行的**总重**（逐试次）。
        ///
        /// 优先取**父行自己的称量值**。存量数据里父行有两种录法，这一个定义同时吃得下：
        ///   · 父重 = 两子之和（11 条，多为 07-20 那轮连测）—— 父行就是这份双组分的称样量；
        ///   · 父重 = 子行一（2 条，含真实记录 87.405.26.13233.01）—— 子行一是父重的副本。
        /// 原先一律取"子行一"，把第一种录法的父行算成了其中一份，外层百分比偏小。
        ///
        /// 父行为 0（3 条）或**小于残留**（数据错）时退化成两子行之和 —— 那是唯一还能自洽的取值。
        /// 非双组分行（子行数为 0）**原值返回**，逐字是改动前的行为。
        ///
        /// ⚠️ 这个值同时喂三处：外层百分比、括号内比例、母行的 MR 加权。改它 = 改这三处，
        /// 别再引入第二套"父行总重"的定义。
        /// </summary>
        private static (decimal Trail1, decimal Trail2) ResolveBicomponentParentGsm(
            float parentGsm1, float parentGsm2, IReadOnlyList<BicomponentSubFiber>? subs)
        {
            var trail1 = (decimal)parentGsm1;
            var trail2 = (decimal)parentGsm2;
            if (subs == null || subs.Count == 0) return (trail1, trail2);

            var (residueGsm1, residueGsm2) = ResidueGsm(subs);

            if (trail1 <= 0 || trail1 < residueGsm1) trail1 = subs.Sum(s => s.GSMTrail1);
            if (trail2 <= 0 || trail2 < residueGsm2) trail2 = subs.Sum(s => s.GSMTrail2);

            return (trail1, trail2);
        }

        /// <summary>
        /// 残留那一份的湿重（逐试次）= **子行最后一个**。
        /// <see cref="ResolveBicomponentParentGsm"/> 与 <see cref="DecomposeBicomponent"/> 共用，
        /// 口径只此一处：与 <see cref="CalculateDissolvedUnits"/> 的"最后一行 own = 当前行自身，
        /// 前面的都是差值"同源。要换成"第一个是残留"是口径变更，不是修 bug。
        /// </summary>
        private static (decimal Trail1, decimal Trail2) ResidueGsm(IReadOnlyList<BicomponentSubFiber> subs)
            => (subs[^1].GSMTrail1, subs[^1].GSMTrail2);

        /// <summary>
        /// 把双组分父行拆成「残留 / 被溶解」两份，返回各自**折干后**的克重（两次称量已合并）。
        ///
        /// **残留 = 子行最后一个** —— 与 <see cref="CalculateDissolvedUnits"/> 的
        /// "最后一行 own = 当前行自身，前面的都是差值"是同一套口径；
        /// 被溶解 = 父行总重 − 残留（父行总重见 <see cref="ResolveBicomponentParentGsm"/>）。
        /// 各按**自己名字**的回潮率折干：干重 = 湿重 × (1 + MR/100)，与
        /// <see cref="CalculateRates"/> 的 `(1+MR)*Correct/100` 同源。
        ///
        /// 拆不出来时返回 (0, 0) 让调用方各自跳过 —— 绝不在数据错（父重小于残留）时硬猜一个比例。
        /// </summary>
        private (decimal ResidueDry, decimal DissolvedDry) DecomposeBicomponent(
            decimal parent1, decimal parent2, IReadOnlyList<BicomponentSubFiber>? subs)
        {
            if (subs == null || subs.Count < 2) return (0m, 0m);

            var residue = subs[^1];
            var (residueGsm1, residueGsm2) = ResidueGsm(subs);
            var dissolvedGsm1 = parent1 - residueGsm1;
            var dissolvedGsm2 = parent2 - residueGsm2;
            if (dissolvedGsm1 < 0 || dissolvedGsm2 < 0) return (0m, 0m);

            var residueMr = LookupMoistureRegain(residue.FiberName);
            var dissolvedMr = LookupMoistureRegain(subs[0].FiberName);

            var residueDry = (residueGsm1 + residueGsm2) * (1m + residueMr / 100m);
            var dissolvedDry = (dissolvedGsm1 + dissolvedGsm2) * (1m + dissolvedMr / 100m);
            return (residueDry, dissolvedDry);
        }

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
                var parentPct = line.Split('%')[0].Trim();
                var fullName = line.Contains("Bicomponent Fiber")
                    ? "Bicomponent Fiber" : "Biconstituent Fiber";
                var shortName = line.Contains("Bicomponent Fiber")
                    ? "bicomponent" : "biconstituent";

                var p1 = subs[0];
                var p2 = subs[1];
                var s1Name = p1.FiberName;
                var s2Name = p2.FiberName;

                // 父行**拆开**算 —— 括号里的比例 = 残留 / (残留 + 被溶解)。拆法见
                // ResidueGsm 与 DecomposeBicomponent，这里只说为什么不能只改一半。
                //
                // 改之前是「子行二折干 ÷ 子行一折干 ×100，再拿 100 去减」，结构上就是
                // 「子行二/子行一」—— 在"子行一 = 父重、子行二 = 残留"那种录法下**碰巧**等于「残留/父重」，所以能一直活着。
                // ⚠️ 只把分子换成真正的"被溶解"质量、分母留着不动，同一份 13233 会翻成
                // 0.5478/0.1077 = 508.4% 与 100−508.4 = −408.4% —— 分子分母必须一起换。
                var (residueDry, dissolvedDry) = DecomposeBicomponent(
                    row.GSMTrail1, row.GSMTrail2, subs);
                var dryTotal = residueDry + dissolvedDry;
                if (dryTotal == 0) continue;

                // 残留是**子行最后一个**（按位置定的，不认纤维名：13233 的残留是 Polyester，
                // 95955 那几条 Polyester 排前面的记录里残留反而是 Polyamide）。
                // 它在下面那串里占的一直是 s2 那一格 —— 变量名是历史包袱，名字与顺序一字不改。
                var residueShare = residueDry / dryTotal * 100m;

                string s1Pct, s2Pct;
                if (format == "F0")
                {
                    var s2Rounded = Math.Round(residueShare, MidpointRounding.AwayFromZero);
                    s2Pct = s2Rounded.ToString("F0");
                    s1Pct = (100m - s2Rounded).ToString("F0");
                }
                else
                {
                    s2Pct = residueShare.ToString("F1");
                    s1Pct = (100m - residueShare).ToString("F1");
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

        // 设备编码常量（对应 Excel L23/O23/R23/L24/O24 —— 五格对五台）
        // 显微镜设备串统一为 `Microscope: SFL-NGB-EQP-XXX`（冒号后带空格 + 设备号用连字符）。
        // 原先两处格式不一致（一处无空格、一处用下划线），2026-09-29 起只剩这一处。
        // 纯报告外观一致性，不涉及模板匹配 —— 模板与已生成 docx 里 `SFL` 均 0 命中。
        //
        // ⚠️ 显微镜**只有一台**，别再按 cellulosic 父槽追加第二台。
        private const string MICROSCOPE = "Microscope: SFL-NGB-EQP-280";
        private const string OVEN = "Oven:SFL-NGB-EQP-164";
        private const string BALANCE = "Balance:SFL-NGB-EQP-061";
        private const string WATER_BATH = "Water bath:SFL-NGB-EQP-046";
        private const string SHAKER = "Shaker:SFL-NGB-EQP-052";

        // Shaker 触发纤维（**含有**其中任一时需化学溶解）
        private static readonly HashSet<string> ShakerTriggerFibers = new(StringComparer.OrdinalIgnoreCase)
        {
            "nylon", "polyamide", "wool"
        };

        // P130 规则：Polyester 前不允许的纤维
        private static readonly HashSet<string> ProhibitedBeforePolyester = new(StringComparer.OrdinalIgnoreCase)
        {
            "nylon", "polyamide", "wool", "silk", "acetate"
        };

        /// <summary>
        /// 设备选型主入口（对应 Excel L23/O23/R23/L24/O24）。
        ///
        /// <para>
        /// 判据是**分析类型**，不是纤维条数：单组分是定性鉴别报告，只给鉴别设备（显微镜）；
        /// 多组分是定量报告，才给烘箱（O23）/天平（R23）/水浴（L24）/摇床（O24）
        /// —— 后四项都服务于"拆分、溶解、烘干后称量"，单组分不称量故不给。
        /// </para>
        /// <para>
        /// ⚠️ 原先这里传的是 <c>orderedFiberNames.Count</c>，形参名还叫 <c>componentCount</c>
        /// —— 名字本身就是错的，已连同该参数一起删除。**别再按"条数"重新引入**：
        /// 单组分记录也可以列多条单纤维（真实记录 87.405.26.12312.01 = Modal + Silk 两条），
        /// 多组分记录也可能只列一条 —— 条数区分不出分析类型，两个方向都会判错。
        /// </para>
        /// </summary>
        private EquipmentSelection SelectEquipment(AnalysisType analysisType, List<string> orderedFiberNames)
        {
            var isMultiple = analysisType == AnalysisType.Multiple;

            return new EquipmentSelection
            {
                // L23 显微镜是**定性鉴别**必用设备 —— 单组分报告同样是一份鉴别报告
                // （FZ/T 01057.3 显微法、ISO/TR 11827 §7.1.1、AATCC TM20 都以显微法为起点），
                // 所以门槛是"有纤维"，**不是**"多组分"。与下面四项刻意不同闸。
                Microscope = orderedFiberNames.Count > 0 ? MICROSCOPE : string.Empty,        // L23
                Oven = isMultiple ? OVEN : string.Empty,                                     // O23
                Balance = isMultiple ? BALANCE : string.Empty,                               // R23
                WaterBath = isMultiple ? SelectWaterBath(orderedFiberNames) : string.Empty,  // L24
                Shaker = isMultiple ? SelectShaker(orderedFiberNames) : string.Empty         // O24
            };
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

            // P131: 任何相邻对中前者为丙烯腈类、后者够格当它的合作方 → 水浴（回退）
            // 这里原先是第三处裸字面量 `f == "acrylic"`，与 ISO -12 / GB .12 两处本属同一语义。
            // 不收编的话，Modacrylic 的方法栏会印 DMF 法（该分部试剂就是 DMF、需要水浴），
            // 设备栏却不给水浴 —— 方法栏与设备栏自相矛盾。三处一起改。
            //
            // 后位判据与那两处**同一条**：方法栏拿不到 -12 时设备栏就不该给水浴（谓词自带 Trim、
            // 空串落空，原先的 IsNullOrWhiteSpace 守卫被它吸收）。
            for (int i = 1; i < fibers.Count; i++)
            {
                if (FiberTokens.IsAcrylicType(fibers[i - 1])
                    && FiberTokens.IsIso1833_12OtherFibre(fibers[i]))
                {
                    return WATER_BATH;
                }
            }

            return string.Empty;
        }

        /// <summary>O24 — 振荡器/化学溶解设备选择</summary>
        private string SelectShaker(List<string> fibers)
        {
            // 只有一种纤维就没有可溶解掉的对象 —— 这道守卫是规则本身需要的，
            // 与拆掉的那道"条数闸"不是一回事（那道管单组分 vs 多组分），别一起删。
            if (fibers.Count < 2) return string.Empty;

            // 含 nylon/polyamide/wool **任一**、且有 ≥2 成分 → Shaker。
            // ⚠️ 只在**父槽层**的扁平列表里找，不下钻子纤维（CellulosicSub / BicomponentSub）。
            // 一条 `Biconstituent Fiber` 就算子纤维里有 Nylon 也不算触发 —— 与
            // GetOrderedFiberSlots「不下钻子层」的既有权衡一致（下钻会同时打乱水浴的相邻对判定）。
            if (fibers.Any(f => ShakerTriggerFibers.Contains(f)))
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