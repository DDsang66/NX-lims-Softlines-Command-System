namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis
{
    /// <summary>
    /// 成分槽位 —— 跨槽叉积配对与组分数计算的输入。
    ///
    /// 为什么需要它：规则表的配对输入有两路，其中一路是 <see cref="IngredientAnalysisCalculation"/> 里
    /// GetOrderedFiberNames() 返回的**扁平名列表**。那个列表有两个刻意的性质、槽位通道不得改动：
    ///   · 含分组父槽字面量（"*cellulosic fibre" / "*Regenerated cellulose fibre"）
    ///     —— 喂 SelectEquipment 的显微镜追加（MICROSCOPE_CELLULOSIC）、以及规则表里两处
    ///     显微镜标准追加，都是**报告内容**
    ///   · 不下钻子层 —— 下钻会打断设备选型（SelectShaker 取 fibers[0]、SelectWaterBath 看相邻对）
    ///     与三元法短路
    ///
    /// 结果是亚麻在业务上只作为 cellulosicSubFibers 录入，**永远走不到配对循环里**，
    /// 于是 ISO 1833-22 / GB/T 2910.22 这两条规则结构上不可能触发（实测 18 条含 linen 的记录，
    /// 100% 只在子层）。所以另开这条**槽位通道**：配对走**跨槽叉积**
    /// （<see cref="FiberStandardChainBuilder.EnumeratePairs"/>），而不是原先的相邻对。
    ///
    /// 计数的口径：父槽**不单列**，被它的子纤维**替换**（1 换 N）。
    /// 故 [Polyester, *cellulosic fibre(Linen/Cotton)] 的成分数是 3（1 + 2），
    /// 既不是只数顶层槽的 2，也不是父子都算的 4。这个数是 <see cref="PairingNames"/> 的长度之和，
    /// 只喂 <see cref="FiberStandardChainBuilder.EffectiveComponentCount"/> 的三元法短路
    /// —— **不是报告上显示的组分数**（报告本来就不显示，那段是死写入）。
    /// </summary>
    /// <param name="Name">槽位自身的纤维名；分组父槽就是 "*cellulosic fibre" 这类字面量</param>
    /// <param name="SubFibers">子纤维名（无子纤维时为空集合）</param>
    internal sealed record FiberSlot(string Name, IReadOnlyList<string> SubFibers)
    {
        /// <summary>无子纤维的普通槽（绝大多数）。</summary>
        internal static FiberSlot Leaf(string name) => new(name, Array.Empty<string>());

        /// <summary>
        /// 该槽参与配对、参与计数时展开成的名字集合：
        /// 有子纤维就**用子纤维**（父槽字面量不出现），否则用自身。
        /// </summary>
        internal IReadOnlyList<string> PairingNames =>
            SubFibers.Count > 0 ? SubFibers : new[] { Name };
    }
}
