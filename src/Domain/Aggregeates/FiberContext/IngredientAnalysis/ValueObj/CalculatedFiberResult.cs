namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.ValueObj
{
    /// <summary>
    /// 纤维项（强类型视图，用于领域计算与测试）
    /// </summary>
    public abstract record CalculatedFiberResult 
    {
        public string Qualitative { get; init; } = string.Empty;
        public string Reagent { get; init; } = string.Empty;
    }


    /// <summary>
    /// 单条纤维项（强类型视图，用于领域计算与测试）
    /// </summary>
    public record SingleCalculatedFiberItem: CalculatedFiberResult
    {
        public string FiberName { get; init; } = string.Empty;
        public string Sample { get; init; } = string.Empty;
        public decimal MoistureRegain { get; init; }
        public decimal Rate { get; init; }
    }
    /// <summary>
    /// 多条纤维项（强类型视图，用于领域计算与测试）
    /// </summary>
    public record MultiCalculatedFiberItem : CalculatedFiberResult
    {
        public string Sample { get; init; } = string.Empty;
        public decimal GSMTrail1 { get; init; }
        public decimal GSMTrail2 { get; init; }
        public decimal RateTrail1 { get; init; } = 100m;
        public decimal RateTrail2 { get; init; } = 100m;
        public decimal Rate { get; init; } = 100m;
        public decimal Avg { get; init; } = 100m;
        public List<MultiFiberRowUnit>? MultiFiberRowUnits { get; init; } = null;
    }

    /// <summary>
    /// 多组分结果单元（强类型视图，用于领域计算与测试）
    /// </summary>
    public record MultiFiberRowUnit
    {
        public string Section { get; init; } = string.Empty;
        public string Sum { get; init; } = string.Empty;
        public decimal GSMTrail1 { get; init; }
        public decimal GSMTrail2 { get; init; }
        public decimal RateTrail1 { get; init; }
        public decimal RateTrail2 { get; init; }
        public decimal Avg { get; init; }
        public decimal Correct { get; init; }
        public decimal MoistureRegain { get; init; }

        /// <summary>
        /// <see cref="MoistureRegain"/> 是不是**纤维表里真有这个数** —— 真值 0 也算有。
        ///
        /// 单开这个标记，是因为报告上「表里就是 0」（如 `Polyurethane` 的 ISO 回潮率 0.00）
        /// 与「根本查不到」（组头行的缩写串 `M/E/S`、表里没有的纤维名、没选标准）
        /// **必须印成两样**：前者印 `0.00%`，后者留空。
        /// 这两件事在 <see cref="MoistureRegain"/> 里都是 `0m`，分不出来。
        ///
        /// 双组分父行恒 false —— 它的加权回潮率只用来算、不上报告，
        /// 别为了"统一"把它改成 true。
        /// </summary>
        public bool MoistureRegainKnown { get; init; }

        public decimal Rate { get; init; }
        public List<CellulosicSubFiber> CellulosicSubFibers { get; init; } = new();
        public List<BicomponentSubFiber> BicomponentSubFibers { get; init; } = new();
    }

}
