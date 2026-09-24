using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis
{
    /// <summary>
    /// 纤维名归类谓词 —— 方法标准链规则表共用。
    ///
    /// **每个谓词都是一张 <see cref="StringComparer.OrdinalIgnoreCase"/> 集合**、入口统一
    /// Trim()。原先是 f == "..." 链，**大小写与首尾空白都敏感**，而调用方并不都做过规范化——
    /// WaterBath 选型传进来的 fibers[i-1] 直接来自 GetOrderedFiberNames()，从未小写化。
    /// 谓词自带大小写不敏感与 Trim 之后，这个隐患在三处调用点一起消失。
    ///
    /// **从聚合根搬出规则表时原样带过来的两处错处，已在这里归位**：
    ///   · 美式 "*cellulosic fiber" → 英式 "*cellulosic fibre"（生产库与前端用的就是这个）
    ///   · 错字 "*re cellulose" → "*Regenerated cellulose fibre"
    ///
    /// **只补标准范围内成立的成员**。判定依据是各分部自己的适用范围，不是"生产里出现过就补"：
    /// 黄麻（jute）虽然是纤维素纤维，但 ISO 1833 系列没有它的分部（1833-15 是黄麻+动物纤维的含氮量法，
    /// 本规则表没有这条），所以 IsCellulosicOrCotton **刻意不收**它——收进去会让
    /// (Jute, Polyester) 命中 -11，而 1833-11 的对象表里没有黄麻。
    /// 同理 Elastodiene 不入 IsElastane（-12/-20 都不含它），故含它的配对一律落空，
    /// 该配什么分部待实验室裁定。
    /// </summary>
    internal static class FiberTokens
    {
        // 带星号的是前端合成/库里的**分组父槽**字面量（其下挂 cellulosicSubFibers 子纤维），不是真实纤维名。
        // 父槽字面量仍会作为配对的相邻元素出现，故两个父槽字面量必须留在集合里。
        private const string SlotCellulosic = "*cellulosic fibre";
        private const string SlotRegenerated = "*Regenerated cellulose fibre";

        /// <summary>蛋白纤维（动物毛 + 丝）。前位命中即在 ISO 侧走 -4（次氯酸盐法）。</summary>
        private static readonly HashSet<string> Animal = new(StringComparer.OrdinalIgnoreCase)
        {
            "wool", "alpaca", "cashmere", "mohair", "rabbit hair", "silk",
            "*animal",
            // 野生蚕丝与动物毛。原实现一律不命中（生产实测 Tasar 6 次，全在开发期）
            "tussah", "tasar", "camel", "yak", "horse hair",
            // 特种动物毛/丝，各 1-6 条（不在 fiber_database 42 行内，回潮率列取不到值）
            "mink", "beaver", "guanaco", "reindeer", "anaphe", "eri", "ailanthus",
        };

        /// <summary>
        /// 羊毛法（16 CFR § 300.3(b)）意义上的"羊毛" —— 亚 5% 也**必须点名带百分比**，
        /// 不并入 "other fiber(s)"。
        ///
        /// 成员照 15 U.S.C. § 68(b) 的定义逐字取：绵羊/羔羊毛，安哥拉山羊的毛（mohair）、
        /// 克什米尔山羊的毛（cashmere），以及所谓特种毛 —— 骆驼（camel）、羊驼（alpaca）、
        /// 美洲驼（llama）、骆马（vicuna）。
        ///
        /// **刻意不是 <see cref="Animal"/>**：那一张是"蛋白纤维"，服务于 ISO 侧的 -4（次氯酸盐法），
        /// 含丝、兔毛、牦牛毛、马尾毛、野蚕丝等。羊毛法的名单比它窄得多 ——
        /// 丝、兔毛、牦牛毛都不在 § 68(b) 的定义里，把它们放进亚 5% 豁免就是超范围。
        /// </summary>
        private static readonly HashSet<string> WoolFamily = new(StringComparer.OrdinalIgnoreCase)
        {
            "wool", "mohair", "cashmere", "camel", "alpaca", "llama", "vicuna",
        };

        /// <summary>弹性纤维。只含 -12 / -20 两个分部适用范围里确实列到的两种。</summary>
        private static readonly HashSet<string> Elastane = new(StringComparer.OrdinalIgnoreCase)
        {
            "elastane", "spandex",
        };

        /// <summary>再生纤维素 + 棉（IsCellulosic 的成员全部落在 ISO 1833-20 的范围内）。</summary>
        private static readonly HashSet<string> Cellulosic = new(StringComparer.OrdinalIgnoreCase)
        {
            "rayon", "viscose", "modal", "lyocell", "cupro", "cotton",
            SlotRegenerated,
        };

        /// <summary>纤维素类（含天然纤维素）。喂 -11 的对象集合与 GB 侧的纤维素判定。</summary>
        private static readonly HashSet<string> CellulosicOrCotton = new(StringComparer.OrdinalIgnoreCase)
        {
            "cotton", "hemp", "linen", "ramie", "paper", "paper yarn",
            // 1833-11 的对象表里有 flax，而库里 Flax / Linen 是同一纤维的两行
            "flax",
            SlotCellulosic,
            // 再生纤维素那 6 个成员的并集（与原实现的 IsCellulosic(f) 调用等价）
            "rayon", "viscose", "modal", "lyocell", "cupro", SlotRegenerated,
        };

        /// <summary>再生纤维素 —— -6（甲酸/氯化锌法）与 -22（甲酸法）的对象纤维。</summary>
        private static readonly HashSet<string> RayonType = new(StringComparer.OrdinalIgnoreCase)
        {
            "rayon", "viscose", "modal", "cupro", "lyocell",
            SlotRegenerated,
        };

        /// <summary>
        /// 丙烯腈类。**这个谓词是新建的**——原先三处（ISO -12、GB .12、WaterBath 回退分支）
        /// 都是裸字面量 f == "acrylic"，Modacrylic 因此全部落空。三处必须一起改，
        /// 否则会出现"方法栏写 DMF 法、设备栏却不给水浴"的自相矛盾。
        /// </summary>
        private static readonly HashSet<string> AcrylicType = new(StringComparer.OrdinalIgnoreCase)
        {
            "acrylic", "modacrylic",
        };

        /// <summary>
        /// **分组父槽** —— 只有它们下面的 cellulosicSubFibers 才有"顶替父槽"的含义
        /// （计数口径见 <see cref="FiberStandardChainBuilder.EffectiveComponentCount"/>）。
        ///
        /// 判定用白名单、而不是"名字以 * 开头"：将来新增分组字面量时应当**显式**决定它能不能展开，
        /// 不能因为前缀相同就静默放开。
        ///
        /// 别的槽挂子纤维是**录入错位** —— 实测 87.405.26.46546.01（id=2077597842830000128）
        /// 把 cotton/linen 挂在了 Polyamide 行上，而同一记录里 *cellulosic fibre
        /// 那行的子纤维是空的。展开这种槽等于凭空造出两个成分：成分数被抬到 3、三元法早退触发、把真实的 -7 挤掉。
        /// </summary>
        private static readonly HashSet<string> CellulosicGroupParent = new(StringComparer.OrdinalIgnoreCase)
        {
            SlotCellulosic, SlotRegenerated,
        };

        /// <summary>
        /// ISO 1833-12:2020（DMF 法）适用范围内的纤维名 —— 对象纤维 ∪ "某些其他纤维"，
        /// 供判定配对里的**后位**纤维用（弹性纤维与丙烯腈类两条兜底都靠它）。
        ///
        /// 对象纤维是 acrylic / certain modacrylics / certain chlorofibres / certain elastanes
        /// （前三者由 <see cref="IsAcrylicType"/>、<see cref="IsElastane"/> 覆盖，这里只补含氯纤维）；
        /// "certain other fibres" 是 cotton / viscose / cupro / modal / lyocell / polyamide / polyester /
        /// polypropylene / elastomultiester / elastolefin / melamine / polypropylene-polyamide bicomponent /
        /// polyacrylate / glass，再加上 wool 与动物毛、丝（由 <see cref="IsAnimal"/> 覆盖）。
        ///
        /// **收法只有一条准则：只收官方范围里逐字列到的名字。** 故刻意不收：
        /// 分组父槽字面量（它们不是纤维名）、通用占位名 Bicomponent Fiber
        /// （-12 列的是 polypropylene/polyamide bicomponent 这一种具体组合，
        /// 占位名并不指明组成，写进去等于替实验室断言）、以及范围外的真实纤维
        /// （acetate / linen / flax / hemp / jute / ramie / paper / elastodiene / olefin /
        /// polyethylene / polyurethane / rubber / metal fibre 等）。它们落空是"按标准来"的结果。
        ///
        /// rayon 与 nylon 是 viscose / polyamide 在 fiber_database 里的
        /// 同义行，照 <see cref="RayonType"/> 的既有口径一并收。
        /// </summary>
        private static readonly HashSet<string> Iso1833_12Others = new(StringComparer.OrdinalIgnoreCase)
        {
            "chlorofibre",

            "cotton", "viscose", "rayon", "cupro", "modal", "lyocell",
            "polyamide", "nylon", "polyester", "polypropylene",
            "elastomultiester", "elastolefin", "melamine", "polyacrylate", "glass",
        };

        internal static bool IsAnimal(string f) => Animal.Contains(Clean(f));
        internal static bool IsElastane(string f) => Elastane.Contains(Clean(f));
        internal static bool IsWoolFamily(string f) => WoolFamily.Contains(Clean(f));
        internal static bool IsCellulosic(string f) => Cellulosic.Contains(Clean(f));
        internal static bool IsCellulosicOrCotton(string f) => CellulosicOrCotton.Contains(Clean(f));
        internal static bool IsRayonType(string f) => RayonType.Contains(Clean(f));
        internal static bool IsAcrylicType(string f) => AcrylicType.Contains(Clean(f));

        /// <summary>是不是分组父槽。决定它下面的 cellulosicSubFibers 该不该顶替它（见集合注释）。</summary>
        internal static bool IsCellulosicGroupParent(string f) => CellulosicGroupParent.Contains(Clean(f));

        /// <summary>
        /// 这个名字是不是出现在 ISO 1833-12:2020 的适用范围里 —— 对象纤维 ∪ "某些其他纤维"
        /// （见 <see cref="Iso1833_12Others"/> 的注释）。
        ///
        /// 三个消费者，都是"后位纤维够不够格跟 -12 的对象纤维配一对"这同一个问题：
        /// LookupSubStandard 的弹性纤维兜底与丙烯腈类兜底，
        /// 以及 LookupGbSubStandard 的丙烯腈类兜底（GB/T 2910.12–2023 的
        /// 第二组分列表与 ISO 侧逐项一致，故共用这一条而不是另建一份 GB 集合）。
        /// </summary>
        internal static bool IsInIso1833_12Scope(string f)
            => IsAcrylicType(f) || IsElastane(f) || IsAnimal(f) || Iso1833_12Others.Contains(Clean(f));

        /// <summary>null 视为空串；首尾空白在此统一剥掉，调用方无须预处理大小写。</summary>
        private static string Clean(string f) => f?.Trim() ?? string.Empty;
    }
}
