using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis
{
    /// <summary>
    /// 损伤系数 d —— 溶解法把某一成分溶掉后，**残留那一方**按方法标准该乘的质量修正系数。
    ///
    /// **问的是"前一个成分被溶掉、本行是残留"这一对，不是"本行是什么纤维"。**
    /// 组内的挂法（对齐原工作簿 多组分 表的 Correct 列）：
    ///   · 每个溶解组的**第一个**成员没有前位，恒 1；
    ///   · 其余成员 = <see cref="Resolve"/>(前一个成员, 本成员)；
    ///   · 拆分行恒 1，组头行没有这一格。
    ///
    /// **两侧都在名单里才生效**：`(粘胶, 棉)` 给 1.03，而 `(棉, 涤)` 给 1.00 ——
    /// 后者的残留方不在名单里。不是"凡是跟在棉后面的成分都吃系数"。
    ///
    /// 取值：
    ///   · 再生纤维素被溶掉后残留**棉** → 1.03、残留**亚麻** → 1.07
    ///     （ISO 1833-6 / GB/T 2910.6 甲酸/氯化锌法，70℃）。苎麻、大麻官方即 1.00，故不列。
    ///   · 丙烯腈类或弹性纤维被溶掉后残留名单内纤维 → 1.01
    ///     （GB/T 2910.12—2023，MOD ISO 1833-12:2020）。
    ///
    /// <see cref="MultiFiberRowUnit.Correct"/> 进公式时 <c>Correct/100</c> 在分子分母里逐字相消，
    /// 所以恒 1 是空操作、真值则按比例改变报告上的每一个百分比（分母跨全部成分行）。
    /// </summary>
    internal static class FiberDamageFactor
    {
        /// <summary>不适用时的 d。与"没填"同义，对公式是空操作。</summary>
        private const decimal None = 1.00m;

        /// <summary>甲酸/氯化锌法（70℃）里棉的 d。</summary>
        private const decimal CottonAfterRegenerated = 1.03m;

        /// <summary>甲酸/氯化锌法里亚麻的 d。</summary>
        private const decimal LinenAfterRegenerated = 1.07m;

        /// <summary>DMF 法（-12 / 2910.12）残留方的 d。</summary>
        private const decimal AfterAcrylicOrElastane = 1.01m;

        /// <summary>
        /// 再生纤维素溶掉后、d ≠ 1 的残留纤维。
        ///
        /// 官方在这个方法里只给**棉** 1.03；**亚麻** 1.07。苎麻、大麻同在该法的混合物范围里，
        /// 但苎麻的 d 官方就是 1.00、大麻未给，故本表不收 —— 它们落 1.00 是按标准来的结果。
        /// Flax 与 Linen 是 fiber_database 里同一纤维的两行，照本模块的既有口径一并收。
        /// </summary>
        private static readonly Dictionary<string, decimal> AfterRayonType = new(StringComparer.OrdinalIgnoreCase)
        {
            ["cotton"] = CottonAfterRegenerated,
            ["linen"] = LinenAfterRegenerated,
            ["flax"] = LinenAfterRegenerated,
        };

        /// <summary>
        /// -12（DMF 法）里 d = 1.01 的残留纤维，逐字取自官方名单：
        /// 绵羊毛、棉、粘胶、铜氨、莫代尔、莱赛尔、聚酰胺、聚酯、弹性涤、三聚氰胺、聚丙烯酸酯。
        ///
        /// **与 <see cref="FiberTokens.Iso1833_12OtherFibres"/> 是两个问题，别合并**：
        /// 那一张是 -12 的**适用范围**（含聚丙烯、弹性烯烃、玻璃，不含羊毛），
        /// 这一张是 **d 值名单**（含羊毛，不含那三个）。名字像、内容不同。
        ///
        /// **羊毛只收绵羊毛本身**，不借 <see cref="FiberTokens.IsWoolFamily"/> ——
        /// 那张表是 16 CFR § 300.3(b) 的羊毛法口径，含羊驼、马海毛、山羊绒等特种毛，
        /// 官方这张 d 表里没有它们。
        ///
        /// 两个**分组父槽字面量**（*cellulosic fibre / *Regenerated cellulose fibre）不在此列，
        /// 由 <see cref="FiberTokens.IsCellulosicGroupParent"/> 那一支并进来 —— 父槽代表其成员，
        /// 原工作簿的名单里也是把它们与真实纤维名并列收的。
        /// </summary>
        private static readonly HashSet<string> DmfResidues = new(StringComparer.OrdinalIgnoreCase)
        {
            "wool", "cotton", "viscose", "rayon", "cupro", "modal", "lyocell",
            "polyamide", "nylon", "polyester", "elastomultiester",
            "melamine", "polyacrylate",
        };

        /// <summary>
        /// 求 <paramref name="residue"/> 作为残留、<paramref name="dissolved"/> 刚被溶掉时的损伤系数。
        /// 查不到、或前位不在任何一张对象名单里，都返回 1.00。
        /// </summary>
        /// <param name="dissolved">组内**前一个**成员（被本组溶剂溶掉的那方）。</param>
        /// <param name="residue">本行纤维（留在残渣里的那方）。</param>
        internal static decimal Resolve(string? dissolved, string? residue)
        {
            // 再生纤维素在前：它是 -6 / -22 的对象纤维，与 -12 的对象集不相交，两支互斥。
            if (FiberTokens.IsRayonType(dissolved))
                return AfterRayonType.TryGetValue(Clean(residue), out var d) ? d : None;

            if (FiberTokens.IsAcrylicType(dissolved) || FiberTokens.IsElastane(dissolved))
                return IsDmfResidue(residue) ? AfterAcrylicOrElastane : None;

            return None;
        }

        private static bool IsDmfResidue(string? f)
            => DmfResidues.Contains(Clean(f)) || FiberTokens.IsCellulosicGroupParent(f);

        private static string Clean(string? f) => f?.Trim() ?? string.Empty;
    }
}
