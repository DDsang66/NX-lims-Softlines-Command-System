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
        public decimal GSMTrail1 { get; init; }
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
        public decimal Rate { get; init; } 
    }

}
