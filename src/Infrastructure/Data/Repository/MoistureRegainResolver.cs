using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    /// <summary>
    /// 回潮率**列的候选链** —— 按方法标准选列，不再"一击定生死"。
    ///
    /// 为什么要有"链"而不是"一个列"：fiber_database 的 7 个回潮率列，每列只收录**该标准体系
    /// 覆盖到的**纤维，体系外的纤维留 0.00 或 NULL。所以"这一列没值"是常态而非异常，
    /// 单一列取值一旦落空，那只纤维就会从整张 map 里消失（见 Pick 的注释）。
    ///
    /// **这里实际修的是优先级，不是链。** 实测：42 行 × 7 列**没有任何 NULL**，
    /// 所以候选链今天一次都不回退——它的价值是"将来有人把某格置 NULL 时，那只纤维不会凭空消失"。
    /// 真正改到生产的是**顺序**：原实现先判 Contains("Korea")、后判 StartsWith("AATCC")，
    /// 于是 AATCC TM20-2021 AATCC TM20A-2021e (Korea)（真实期 1 条）走的是 KOR 列——
    /// 而 KOR 列 42 行里有 25 行是 0.00（Wool/Alpaca/Cashmere/Elastane/Flax… 全是真值列上的 0），
    /// 那条记录的 Spandex 因此取到 0.00 而非 AATCC 列的 1.30。
    /// AATCC 在前、Korea 降为兜底，正是按"哪列有真值"定的。
    /// </summary>
    internal static class MoistureRegainResolver
    {
        /// <summary>七个回潮率列。Iso 同时充当默认列与通用兜底。</summary>
        internal enum MrColumn
        {
            Iso,
            Aatcc,
            Kor,
            Can,
            Gb,
            Cns,
            Jis,
        }

        /// <summary>
        /// 按方法标准串给出候选列链，**按优先级从高到低**。
        ///
        /// 口径与原实现保持一致的地方：只认 `StartsWith` 的体系前缀（`AATCC`/`CAN`/`FZ/T`/`GB/T`/`CNS`/`JIS`），
        /// 其余一律 Iso。所以逗号多值串（如 ISO1833,AATCC TM20-2021，真实期 5 条）
        /// **仍走 Iso** —— 它首段是 ISO1833，与改动前同值。多值拆链是方法栏的事，
        /// 不改变这里"取第一个标准定回潮率列"的口径。
        /// </summary>
        internal static IReadOnlyList<MrColumn> Resolve(string? standard)
        {
            var s = standard?.Trim() ?? string.Empty;

            // 顺序即优先级：AATCC 在 Korea **之前** —— 见类型注释，KOR 列稀疏。
            if (s.StartsWith("AATCC", StringComparison.OrdinalIgnoreCase))
                return new[] { MrColumn.Aatcc, MrColumn.Kor, MrColumn.Iso };

            // 非 AATCC 的 Korea 记录（今天生产 0 条，留着是为了语义完整，不是为存量）
            if (s.Contains("Korea", StringComparison.OrdinalIgnoreCase))
                return new[] { MrColumn.Kor, MrColumn.Iso };

            if (s.StartsWith("CAN", StringComparison.OrdinalIgnoreCase))
                return new[] { MrColumn.Can, MrColumn.Iso };

            if (s.StartsWith("FZ/T", StringComparison.OrdinalIgnoreCase)
                || s.StartsWith("GB/T", StringComparison.OrdinalIgnoreCase))
                return new[] { MrColumn.Gb, MrColumn.Iso };

            if (s.StartsWith("CNS", StringComparison.OrdinalIgnoreCase))
                return new[] { MrColumn.Cns, MrColumn.Iso };

            if (s.StartsWith("JIS", StringComparison.OrdinalIgnoreCase))
                return new[] { MrColumn.Jis, MrColumn.Iso };

            return new[] { MrColumn.Iso };
        }

        /// <summary>
        /// 按候选链取该纤维的回潮率：**逐个列试，返回第一个非 NULL**；全落空则 null。
        ///
        /// ⚠️ **只跳 NULL，不跳 0.00。** 0.00 是本表"该标准未收录此纤维"的写法，
        /// 看上去也该跳过——但把它也当落空，就等于拿**另一个标准体系的值顶替**（如 ISO 记录里的
        /// Polyurethane 会从 Iso 的 0.00 跳到 Kor 的 1.00），那是静默的跨体系替换，报告上不留痕。
        /// 故这里的口径是**只跳 NULL**：宁可给 0.00，也不悄悄换体系。
        /// </summary>
        internal static decimal? Pick(FiberDatabase fiber, IReadOnlyList<MrColumn> chain)
        {
            foreach (var column in chain)
            {
                var value = Read(fiber, column);
                if (value.HasValue) return value;
            }
            return null;
        }

        private static decimal? Read(FiberDatabase f, MrColumn column) => column switch
        {
            MrColumn.Iso => f.MoistureRegainIso,
            MrColumn.Aatcc => f.MoistureRegainAatcc,
            MrColumn.Kor => f.MoistureRegainKor,
            MrColumn.Can => f.MoistureRegainCan,
            MrColumn.Gb => f.MoistureRegainGb,
            MrColumn.Cns => f.MoistureRegainCns,
            MrColumn.Jis => f.MoistureRegainJis,
            _ => null,
        };
    }
}
