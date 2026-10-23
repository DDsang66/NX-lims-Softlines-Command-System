using System;
using System.Collections.Generic;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.ValueObj;

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
    ///   · 残留方是**分组父槽** `*cellulosic fibre` 时，它自己没有 d，按录的子纤维取：
    ///     有棉 → 1.03（棉、亚麻同时有也取 1.03），没棉只有亚麻 → 1.07，
    ///     其余 / 一条子纤维都没录 → 1.00（不猜组成）。见 <see cref="Resolve(string?, string?, IReadOnlyCollection{CellulosicSubFiber}?)"/>。
    ///
    /// **只此一支**：丙烯腈类的 DMF 1.01 已按 2026-10-23 裁定整支删除（纸件工作簿没有
    /// 1.01 这一档），丙烯腈类前位一律按 1.00 计；方法栏照常印 -12 / 2910.12，删的只是 d。
    ///
    /// <see cref="MultiFiberRowUnit.Correct"/> **不是**可以独立乘进百分比公式的权重：
    /// <c>1.03</c> 表示残留方质量比称出来的重 3%，这 3% 要在**同组的溶解方**身上扣回来
    /// （<c>IngredientAnalysisCalculation.CalculateDissolvedUnits</c> 的 <c>DeductedAvg</c>），
    /// 组内合计恒 100%（GB/T 2910.1 / ISO 1833-1 的 <c>P₂ = 100 − P₁</c>）。
    /// </summary>
    internal static class FiberDamageFactor
    {
        /// <summary>不适用时的 d。与"没填"同义，对公式是空操作。</summary>
        private const decimal None = 1.00m;

        /// <summary>甲酸/氯化锌法（70℃）里棉的 d。</summary>
        private const decimal CottonAfterRegenerated = 1.03m;

        /// <summary>甲酸/氯化锌法里亚麻的 d。</summary>
        private const decimal LinenAfterRegenerated = 1.07m;

        /// <summary>
        /// 再生纤维素溶掉后、d ≠ 1 的残留纤维。
        ///
        /// 官方在这个方法里只给**棉** 1.03；**亚麻** 1.07。苎麻、大麻同在该法的混合物范围里，
        /// 但苎麻的 d 官方就是 1.00、大麻未给，故本表不收 —— 它们落 1.00 是按标准来的结果。
        /// Flax 与 Linen 是 fiber_database 里同一纤维的两行，照本模块的既有口径一并收。
        ///
        /// **分组父槽字面量不在本表**：父槽本身没有 d，要按它下面录的子纤维取，
        /// 走 <see cref="Resolve(string?, string?, IReadOnlyCollection{CellulosicSubFiber}?)"/> 里那条分支。
        /// </summary>
        private static readonly Dictionary<string, decimal> AfterRayonType = new(StringComparer.OrdinalIgnoreCase)
        {
            ["cotton"] = CottonAfterRegenerated,
            ["linen"] = LinenAfterRegenerated,
            ["flax"] = LinenAfterRegenerated,
        };

        /// <summary>
        /// 求 <paramref name="residue"/> 作为残留、<paramref name="dissolved"/> 刚被溶掉时的损伤系数。
        /// 查不到、或前位不是再生纤维素，都返回 1.00。
        /// </summary>
        /// <param name="dissolved">组内**前一个**成员（被本组溶剂溶掉的那方）。</param>
        /// <param name="residue">本行纤维（留在残渣里的那方）。</param>
        internal static decimal Resolve(string? dissolved, string? residue)
            => Resolve(dissolved, residue, null);

        /// <summary>
        /// 同上；<paramref name="residueSubFibers"/> 只在残留方是**分组父槽**时用得上。
        ///
        /// 分组父槽自己没有 d（`*cellulosic fibre` 不在任何一张官方表里），它的 d 取自下面录的子纤维：
        /// **有棉就 1.03**（棉、亚麻同时有也取 1.03 —— 棉这一档先把系数定下来，与份额无关），
        /// **没棉、只有亚麻才 1.07**，其余（苎麻、大麻等官方即 1.00 的）落 1.00。
        /// 一条子纤维都没录时**不猜组成**，同样落 1.00。
        ///
        /// 父槽成员分 1.03 / 1.07 两档，**必须看组成**；别改回按父槽字面量查字典。
        /// </summary>
        /// <param name="dissolved">组内**前一个**成员（被本组溶剂溶掉的那方）。</param>
        /// <param name="residue">本行纤维（留在残渣里的那方）。</param>
        /// <param name="residueSubFibers">本行是分组父槽时，它下面录的子纤维；其他纤维可传 null。</param>
        internal static decimal Resolve(
            string? dissolved, string? residue, IReadOnlyCollection<CellulosicSubFiber>? residueSubFibers)
        {
            // 再生纤维素是 -6 / -22 的对象纤维。
            if (FiberTokens.IsRayonType(dissolved))
            {
                if (FiberTokens.IsCellulosicGroupParent(residue) && residueSubFibers is { Count: > 0 })
                    return ResolveGroupParent(dissolved, residueSubFibers);

                return AfterRayonType.TryGetValue(Clean(residue), out var d) ? d : None;
            }

            return None;
        }

        /// <summary>
        /// 分组父槽的 d：棉优先，其次亚麻，都没命中才 1.00。
        /// 只看子纤维**有没有**，不看份额 —— 见 <see cref="Resolve(string?, string?, IReadOnlyCollection{CellulosicSubFiber}?)"/>。
        /// </summary>
        private static decimal ResolveGroupParent(string? dissolved, IReadOnlyCollection<CellulosicSubFiber> subFibers)
        {
            var hasLinen = false;

            foreach (var sub in subFibers)
            {
                var d = Resolve(dissolved, sub.FiberName);

                if (d == CottonAfterRegenerated) return CottonAfterRegenerated;
                if (d == LinenAfterRegenerated) hasLinen = true;
            }

            return hasLinen ? LinenAfterRegenerated : None;
        }

        private static string Clean(string? f) => f?.Trim() ?? string.Empty;
    }
}
