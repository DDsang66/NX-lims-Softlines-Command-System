using System;
using System.Collections.Generic;
using System.Linq;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis
{
    /// <summary>
    /// 方法标准链规则表 —— 由 <see cref="IngredientAnalysisCalculation"/> 搬出，规则在此维护。
    ///
    /// 这一层改的都是**规则与常量**，不改结构：
    ///   · ISO1833_20 年份 2020 → **2018**（2020 只是各国采标年份，不存在这个标准号）；
    ///     DIN1833_D5x 同时改成由常量拼装，消掉"复制了一份字面量"的隐患
    ///   · 显微镜法串 FZ/T 30003-2009 → **2024**（FZ/T 01057.3 仍是 2007，2025 版 2027 才实施）
    ///   · ISO 侧：-1（试验通则，不是纤维对的定量方法）改 -20；-22 上移到 -6 之前；
    ///     -6 按官方范围收紧；补 flax（不补 ramie，1833-22:2020 只有 flax）
    ///   · GB 侧：删除"聚酯在前一律给 2910.24"的兜底，换成涤氨双向 2910.20；补纤维素 × 弹性纤维 2910.20
    ///   · 丙烯腈三处裸字面量（ISO -12、GB .12、设备选型）统一走 <see cref="FiberTokens.IsAcrylicType"/>
    ///
    /// **丙烯腈守卫**：ISO 1833-20 / GB/T 2910.20 官方都声明"不适用于聚丙烯腈纤维同时存在的情况"，
    /// 所以这两条规则必须看到**整条纤维列表**，不能只看当前这一对——故两个查表函数都收 hasAcrylic。
    /// 守卫生效时退回 -12（1833-20 官方注列出的备选方法），而不是落空：否则
    /// (Elastane, 纤维素) 在含丙烯腈时会落空、(纤维素, Elastane) 却不会，又变回
    /// "同一对因录入顺序给不同结果"——凡成对的判定，两个方向必须一致。
    /// </summary>
    internal static class FiberStandardChainBuilder
    {
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
        private const string ISO1833_20 = "ISO1833-20:2018";
        private const string ISO1833_22 = "ISO1833-22:2020";
        // GB/T 2910.x 子标准常量（对应 fdb B 列）。分隔符是 – (U+2013)，与原表一致。
        private const string GB2910_1 = "GB/T 2910.1–2009";
        private const string GB2910_2 = "GB/T 2910.2–2009";
        private const string GB2910_3 = "GB/T 2910.3–2009";
        private const string GB2910_4 = "GB/T 2910.4–2022";
        private const string GB2910_6 = "GB/T 2910.6–2009";
        private const string GB2910_7 = "GB/T 2910.7–2009";
        private const string GB2910_11 = "GB/T 2910.11–2024";
        private const string GB2910_12 = "GB/T 2910.12–2023";
        private const string GB2910_18 = "GB/T 2910.18–2009";
        private const string GB2910_20 = "GB/T 2910.20–2009";
        private const string GB2910_22 = "GB/T 2910.22–2009";
        private const string FZ01026 = "FZ/T 01026–2017";

        /// <summary>显微镜法串。FZ/T 30003 原文就是连字符，与旁边的 FZ/T 01057.3–2007 不同，照抄不"统一"。</summary>
        private const string MICROSCOPE_GB_CHAIN = "FZ/T 01057.3–2007 / FZ/T 30003-2024";

        /// <summary>ISO 1833 各分部常量的集合 —— 由常量拼装，避免"改了常量忘了这里"。</summary>
        private static readonly HashSet<string> DIN1833_D5x = new(StringComparer.OrdinalIgnoreCase)
        {
            ISO1833_1, ISO1833_2, ISO1833_3, ISO1833_4, ISO1833_6, ISO1833_7,
            ISO1833_11, ISO1833_12, ISO1833_18, ISO1833_20, ISO1833_22,
        };

        /// <summary>
        /// Excel L4+L6: 根据标准体系和成分对自动拼接方法标准链。
        ///
        /// **两参数重载 = 全部槽都是叶子槽**（无子纤维）。无子纤维的记录全走这条，
        /// 输出与引入槽位通道之前**逐字一致**——这本身就是槽位通道的行为契约：
        /// "没有子纤维时，槽位通道不产生任何新配对"。
        /// 需要子纤维参与配对（亚麻那一类）才用三参数重载。
        /// </summary>
        internal static string BuildMethodString(string standard, List<string> fibers)
            => BuildMethodString(standard, fibers, fibers.Select(FiberSlot.Leaf).ToList());

        /// <summary>Excel L4+L6: 根据标准体系和成分对自动拼接方法标准链（含子纤维槽位）。</summary>
        /// <remarks>
        /// **这里原先有一段"按逗号切分 → 逐段建链 → 空格合并"的分支，已整段删除。**
        /// 它**从生产路径不可达** —— 适配器的 ParseMethods 早已把多值 method 切成 Methods
        /// 列表，传进来的永远是单值；只有若干部测试绕过适配器直接递逗号串才走得到。
        /// 改成"一个标准一份、最后合并 docx"之后逗号串根本不存在了
        /// （那条路的用例已改写为在聚合根层断言 StandardResults）。
        /// 那句 IsNullOrWhiteSpace 守卫要留着：
        /// <see cref="IngredientAnalysisCalculation.StandardsToRender"/> 在 Methods 为空时退化成
        /// **单个空标准**，走到这里就该返回空串，不是抛异常。
        /// </remarks>
        internal static string BuildMethodString(string standard, List<string> fibers, IReadOnlyList<FiberSlot> slots)
        {
            if (string.IsNullOrWhiteSpace(standard)) return string.Empty;

            return BuildSingleStdChain(standard, fibers, slots);
        }

        /// <summary>
        /// 单个标准的链 —— 这是**唯一**路径，不再有"多值拆链"那种调用形态。
        ///
        /// 两个刻意的边界（别顺手"修"）：
        ///   · **不按空格切分** —— AATCC TM20-2021  AATCC TM20A-2021e（双空格，27 条真实记录）
        ///     是 AATCC 的**复合方法名**（TM20 + TM20A 是一套），不是两个标准，必须整串透传。
        ///   · **顺序由 Methods 决定** —— 多标准的先后就是分析员的勾选顺序，不做规范化。
        /// </summary>
        private static string BuildSingleStdChain(string standard, List<string> fibers, IReadOnlyList<FiberSlot> slots)
        {
            if (string.IsNullOrWhiteSpace(standard)) return string.Empty;

            var isIso = standard.Equals("ISO1833", StringComparison.OrdinalIgnoreCase);
            var isDin = standard.Equals("DIN EN ISO 1833", StringComparison.OrdinalIgnoreCase);
            var isGb = standard.StartsWith("FZ/T", StringComparison.OrdinalIgnoreCase)
                    || standard.StartsWith("GB/T", StringComparison.OrdinalIgnoreCase);

            // 非 ISO/DIN/GB：直接返回原值（Regulation / CAN / CNS / JIS 一律逐字透传）
            if (!isIso && !isDin && !isGb) return standard;

            // 丙烯腈守卫要看到**整条列表**，不是当前这一对 —— 在此算一次，传给两个查表函数
            var hasAcrylic = fibers.Any(FiberTokens.IsAcrylicType);

            // 分组父槽字面量是否在列表里 —— 决定显微镜法那一串加不加。
            // 三条消费者（SelectEquipment 的显微镜追加、这里两处）都读**扁平列表**，
            // 所以槽位通道不改 GetOrderedFiberNames() 的返回内容（父槽字面量必须留在里面），
            // 这两条判定因此逐字未动。
            var hasCellulosicParent = fibers.Any(f => f == "*cellulosic fibre" || f == "*Regenerated cellulose fibre");

            // 三元法短路用的是**成分数**，父槽被其子纤维替换（1 换 N），不是顶层槽数。
            // 无子纤维时它恒等于 slots.Count == fibers.Count，所以那 256 条输出逐字不变。
            var count = EffectiveComponentCount(slots);

            var parts = new List<string>();

            // ========== ISO/DIN ==========
            if (isIso || isDin)
            {
                var qualitative = isIso ? ISO_QUALITATIVE : DIN_QUALITATIVE;
                var ternary = isIso ? ISO1833_2 : "DIN EN ISO 1833-2:2020";

                // 这 3 个成分是不是**分组父槽展开**得来的（计数口径见 EffectiveComponentCount）。
                // 它决定 count == 3 时走"只给 -2"还是"分部与 -2 同时给"，见下面的短路。
                var expanded = slots.Any(s => s.SubFibers.Count > 0);

                parts.Add(qualitative);

                var subStandards = new List<string>();
                foreach (var (first, second) in EnumeratePairs(slots))
                {
                    var s = LookupSubStandard(first, second, hasAcrylic);
                    if (!string.IsNullOrEmpty(s) && !subStandards.Contains(s))
                        subStandards.Add(s);
                }

                // 三元法短路。**两种情形必须分开**：
                //
                //   · **三个独立成分**（没有槽展开）→ 只给 `-2`。这是规则表搬出以来的既有设计，
                //     `[Cotton, Modal, Elastane]` 逐字不变，是回归铁律，也是"无子纤维的记录
                //     输出逐字不变"这条槽位通道契约的落点。`subStandards` 在这里算出来但不输出。
                //
                //   · **3 是分组父槽展开得来的**（父槽 + N 子纤维 + 别的槽）→ 分部与 `-2` 同时给。
                //     实测 `87.405.26.79566.01`（同一纤维组成的 GB 记录）给的是
                //     `GB/T 2910.20–2009` + `GB/T 2910.2–2009` + 显微镜串**三样俱全** ——
                //     GB 侧是累加式，ISO 侧的独占 `return` 是全模块唯一"三元就把分部全扔"的地方。
                //     "两侧行为一致"这个印象是错的（GB 从来都给分部），这里对齐到信息更全的那一侧。
                //
                // 显微镜串不能在早退里被吞掉 —— 上面两支都成立：独占返回的那支自己带上它，
                // 累加的那支在末尾追加。
                if (count == 3 && !expanded)
                {
                    return hasCellulosicParent
                        ? $"{qualitative} {ternary} ISO 20705:2019"
                        : $"{qualitative} {ternary}";
                }

                parts.AddRange(subStandards);

                if (isDin)
                    parts = parts.Select(p => DIN1833_D5x.Contains(p) ? "DIN EN " + p : p).ToList();

                // 展开得来的三元：分部在前、`-2` 在后 —— 与 GB 侧 `…2910.20 …2910.2` 同序
                if (count == 3)
                    parts.Add(ternary);

                if (hasCellulosicParent)
                    parts.Add("ISO 20705:2019");
            }

            // ========== GB (FZ/T / GB/T) ==========
            if (isGb)
            {
                parts.Add(standard);

                // T129: 有拆分列且无 elastane → GB/T 2910.1
                var hasElastane = fibers.Any(FiberTokens.IsElastane);
                if (!hasElastane)
                    parts.Add(GB2910_1);

                // T130: GB 成分对 → GB 子标准
                var gbSubs = new HashSet<string>();
                foreach (var (first, second) in EnumeratePairs(slots))
                {
                    var g = LookupGbSubStandard(first, second, hasAcrylic);
                    if (!string.IsNullOrEmpty(g))
                        gbSubs.Add(g);
                }
                parts.AddRange(gbSubs);

                // T131: 3组分 → GB/T 2910.2 / >3组分 → FZ/T 01026
                if (count == 3)
                    parts.Add(GB2910_2);
                else if (count > 3)
                    parts.Add(FZ01026);

                // cellulosic
                if (hasCellulosicParent)
                    parts.Add(MICROSCOPE_GB_CHAIN);
            }

            return string.Join(" ", parts);
        }

        /// <summary>
        /// 成分数 —— **父槽被其子纤维替换（1 换 N）**，不是顶层槽数、也不是父子都算。
        /// 只喂三元法短路（ISO 三组分早退 / GB 的 2910.2 与 FZ/T 01026）。
        ///
        /// [Polyester, *cellulosic fibre(Linen/Cotton)] → **3**（既不是 2 也不是 4）；
        /// [Cotton, Modal, Elastane]（无子纤维）→ 3，与引入槽位通道之前同值 —— 回归铁律。
        ///
        /// **不是报告上显示的组分数**：那个数在模板里根本没有书签，是死写入。
        /// </summary>
        internal static int EffectiveComponentCount(IReadOnlyList<FiberSlot> slots)
            => slots.Sum(s => s.PairingNames.Count);

        /// <summary>
        /// 参与查表的**有序对**枚举 —— 两段合起来：
        ///
        /// <list type="number">
        /// <item>**相邻对，取槽名** —— 与引入槽位通道之前逐字同源。父槽字面量仍作为配对元素出现在这里
        ///   （GetOrderedFiberNames() 里那个字面量没被拿掉），所以无子纤维的记录
        ///   第二段一次都不执行、输出**逐字不变**。</item>
        /// <item>**子纤维 × 其它槽** —— 只从**含子纤维的槽**出发，与其它每个槽的
        ///   <see cref="FiberSlot.PairingNames"/> **双向**配对。没有子纤维的槽之间不会凭空多出配对：
        ///   这是 C 与"全叉积"的分界，也是它只动 8 条、而全叉积要动 89 条的原因。</item>
        /// </list>
        ///
        /// 第 2 段为什么必须双向：查表函数按 (first, second) 有向，而"谁是前位纤维"
        /// 由前端拆分列/溶解列的表单结构决定，不是分析员的选择 —— 单向配会让同一对因录入顺序给不同分部
        /// （查表函数里那几处"任意顺序"的判定修的正是这一类）。子纤维的方向更无从谈起：它在表单里只有一个格子。
        /// </summary>
        internal static IEnumerable<(string First, string Second)> EnumeratePairs(IReadOnlyList<FiberSlot> slots)
        {
            for (int i = 0; i < slots.Count - 1; i++)
                yield return (slots[i].Name, slots[i + 1].Name);

            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].SubFibers.Count == 0) continue;

                for (int j = 0; j < slots.Count; j++)
                {
                    if (i == j) continue;

                    foreach (var sub in slots[i].PairingNames)
                        foreach (var other in slots[j].PairingNames)
                        {
                            yield return (sub, other);
                            yield return (other, sub);
                        }
                }
            }
        }

        /// <summary>
        /// 对相邻成分对查表返回 ISO1833 子标准编号。
        /// <paramref name="hasAcrylic"/>：整条纤维列表里是否含丙烯腈类纤维，见类型注释的「丙烯腈守卫」。
        /// </summary>
        internal static string LookupSubStandard(string first, string second, bool hasAcrylic)
        {
            var f = first?.Trim().ToLowerInvariant() ?? string.Empty;
            var s = second?.Trim().ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;

            // 纤维素 × 弹性纤维（**任意顺序**）→ -20
            // 这一条把两个方向并成一处：原先 (纤维素, Elastane) 返回 -1（1833-1 是"试验通则"，
            // 不是纤维对的定量方法），而反序 (Elastane, 纤维素) 走下面的 -12 —— 同一对因录入顺序给不同分部。
            // IsCellulosic 的成员全部落在 1833-20 的官方范围内。
            if ((FiberTokens.IsCellulosic(f) && FiberTokens.IsElastane(s))
             || (FiberTokens.IsElastane(f) && FiberTokens.IsCellulosic(s)))
                return hasAcrylic ? ISO1833_12 : ISO1833_20;

            // Silk + wool/cashmere → -18
            if (f == "silk" && (s == "wool" || s == "cashmere"))
                return ISO1833_18;

            // wool/animal + any → -4
            if (FiberTokens.IsAnimal(f))
                return ISO1833_4;

            // polyester + elastane（涤氨，任意顺序）→ -20（DMAc 法）
            if ((f == "polyester" && FiberTokens.IsElastane(s))
             || (FiberTokens.IsElastane(f) && s == "polyester"))
                return hasAcrylic ? ISO1833_12 : ISO1833_20;

            // 弹性纤维在前 → -12（DMF 法），**但后位纤维必须落在 -12 的适用范围内**（按官方范围收紧）。
            //
            // 原先是 `if (IsElastane(f)) return ISO1833_12;` —— 一个不看后位的兜底，
            // 于是 (Spandex, Linen) 也拿到 -12，而 linen 在 **-12 与 -20 的官方范围里都不存在**；
            // 反序 (Linen, Spandex) 却落空 —— 同一对因录入顺序给不同答案，正是要消灭的那类。
            //
            // 收紧后两向都落空，与标准一致。范围外的名字由 FiberTokens.IsInIso1833_12Scope 判定，
            // 收法见那个集合的注释（只收官方逐字列到的名字）。
            //
            // 注意与前几条的分工：(Elastane, 纤维素) 与 (Elastane, Polyester) 走上面的 -20，
            // 根本到不了这里；(Elastane, Acrylic/Modacrylic) 是 -12 的对象纤维、留在 -12。
            if (FiberTokens.IsElastane(f) && FiberTokens.IsInIso1833_12Scope(s))
                return ISO1833_12;

            // polyamide/nylon + any → -7
            if (f == "polyamide" || f == "nylon")
                return ISO1833_7;

            // 丙烯腈类在前 → -12（DMF 法）。谓词含 Modacrylic（1833-12 适用范围含 "certain modacrylics"）。
            //
            // **后位同样要落在 -12 的适用范围内**（与上面弹性纤维兜底同族）。原先是 `+ any`，
            // 于是 (Modacrylic, Elastodiene) 也拿到 -12，而 elastodiene 既不是 -12 的对象纤维、
            // 也不在 "certain other fibres" 里；反序 (Elastodiene, Modacrylic) 却落空 ——
            // 同一对因录入顺序给不同答案。收紧后两向都落空，与标准一致。
            //
            // 范围外的名字由 FiberTokens.IsInIso1833_12Scope 判定（与弹性纤维兜底共用同一条，收法见那个集合的注释）。
            if (FiberTokens.IsAcrylicType(f) && FiberTokens.IsInIso1833_12Scope(s))
                return ISO1833_12;

            // 再生纤维素 + linen/flax → -22（甲酸法）
            // **必须在 -6 之前**：原先排在 -6 后面，而 -6 的旧条件 IsCellulosic(f) && IsCellulosicOrCotton(s)
            // 对 (Viscose, Linen) 成立，于是 -22 永远轮不到、结构上不可达。
            // 补 flax 是因为库里 Flax / Linen 是同一纤维的两行；**不补 ramie**（1833-22:2020 官方范围只有 flax）。
            if (FiberTokens.IsRayonType(f) && (s == "linen" || s == "flax"))
                return ISO1833_22;

            // 再生纤维素 + cotton 等 → -6（甲酸/氯化锌法）
            // 按官方适用范围收紧：对象纤维是 viscose/certain cupro/modal/lyocell（= IsRayonType），
            // 合作纤维是 cotton（该分部正是为它制定的）+ polypropylene / elastolefin / melamine。
            // **不含** linen / ramie / hemp / paper；而且 cotton 在这里是**合作方**、不是对象纤维，
            // 所以 (Cotton, Modal) 这类反序输入从今天起落空（按标准是对的，属"输出变少"，见第 3 层导出）。
            if (FiberTokens.IsRayonType(f) && (s == "cotton" || s == "polypropylene" || s == "elastolefin" || s == "melamine"))
                return ISO1833_6;

            // cellulosic/cotton + elastomultiester/polyester → -11
            if (FiberTokens.IsCellulosicOrCotton(f) && (s == "elastomultiester" || s == "polyester"))
                return ISO1833_11;

            // acetate alone → -3
            if (f == "acetate")
                return ISO1833_3;

            return string.Empty;
        }

        /// <summary>
        /// GB版成分对→子标准号映射（数据库B列，Excel N129-N132）。
        /// <paramref name="hasAcrylic"/> 与 ISO 侧同一条守卫。
        /// </summary>
        internal static string LookupGbSubStandard(string first, string second, bool hasAcrylic)
        {
            var f = first?.Trim().ToLowerInvariant() ?? string.Empty;
            var s = second?.Trim().ToLowerInvariant() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;

            // Silk + wool → GB/T 2910.18
            if (f == "silk" && s == "wool")
                return GB2910_18;

            // wool/Silk + 非spandex/elastane → GB/T 2910.4
            if (FiberTokens.IsAnimal(f) && s != "spandex" && s != "elastane")
                return GB2910_4;

            // polyamide/nylon + 非spandex/elastane → GB/T 2910.7
            if ((f == "polyamide" || f == "nylon") && s != "spandex" && s != "elastane")
                return GB2910_7;

            // 丙烯腈类在前 → GB/T 2910.12（谓词含 Modacrylic），后位同样按范围收紧
            //
            // **与 ISO 侧共用同一条范围判定**：GB/T 2910.12–2023 是 MOD ISO 1833-12:2020
            // （2023-08-06 发布 / 2024-03-01 实施，代替 2009 版），其第二组分列表逐项一致 ——
            // 绵羊毛、其他动物毛、桑蚕丝、棉、粘胶、铜氨、莫代尔、莱赛尔、聚酰胺、聚酯、聚丙烯、
            // 聚酯复合弹性纤维、聚烯烃弹性纤维、三聚氰胺、聚丙烯/聚酰胺复合纤维、聚丙烯酸酯、玻璃。
            // 故不另建一份 GB 专用集合（两份迟早会漂）。
            if (FiberTokens.IsAcrylicType(f) && FiberTokens.IsInIso1833_12Scope(s))
                return GB2910_12;

            // rayon系 + cotton → GB/T 2910.6
            if (FiberTokens.IsRayonType(f) && s == "cotton")
                return GB2910_6;

            // 纤维素 × 弹性纤维（**任意顺序**）→ GB/T 2910.20 —— 与 ISO 侧同一条对称
            if ((FiberTokens.IsCellulosic(f) && FiberTokens.IsElastane(s))
             || (FiberTokens.IsElastane(f) && FiberTokens.IsCellulosic(s)))
                return hasAcrylic ? string.Empty : GB2910_20;

            // polyester + elastane（涤氨，任意顺序）→ GB/T 2910.20 —— 与 ISO 侧的涤氨规则逐字对称
            // 这一条取代了原先的 `if (f == "polyester") return GB2910_24;` 兜底：
            // 2910.24 的适用范围不是"任意后位纤维"，凡聚酯在前一律给它本身就是错的。
            if ((f == "polyester" && FiberTokens.IsElastane(s))
             || (FiberTokens.IsElastane(f) && s == "polyester"))
                return hasAcrylic ? string.Empty : GB2910_20;

            // cellulosic/cotton/acetate/linen/ramie + polyester → GB/T 2910.11
            if ((FiberTokens.IsCellulosicOrCotton(f) || f == "acetate" || f == "linen" || f == "ramie") && s == "polyester")
                return GB2910_11;

            // rayon系 + linen/ramie/flax → GB/T 2910.22
            // 中国版把苎麻扩了进来、ISO 侧没有（两侧刻意不对称）；而 `flax` 与 `linen`
            // 是 fiber_database 里**同一纤维的两行**（ISO 侧已补），这里必须同步补——
            // 否则同一纤维录成 Linen 给 2910.22、录成 Flax 就落空，结果由拼写决定。
            if (FiberTokens.IsRayonType(f) && (s == "linen" || s == "ramie" || s == "flax"))
                return GB2910_22;

            // acetate → GB/T 2910.3
            if (f == "acetate")
                return GB2910_3;

            return string.Empty;
        }
    }
}
