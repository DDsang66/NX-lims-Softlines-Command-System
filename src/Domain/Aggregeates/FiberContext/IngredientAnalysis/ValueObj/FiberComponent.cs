using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.Enums;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.ValueObj
{
    /// <summary>
    /// 成分抽象（值对象基类）
    /// </summary>
    public abstract record FiberComponent
    {
        public abstract AnalysisType Type { get; }
        public string FiberName { get; init; } = string.Empty;
    }

    /// <summary>
    /// 单组分成分
    /// </summary>
    public record SingleFiberComponent : FiberComponent
    {
        public override AnalysisType Type => AnalysisType.Single;
        public string Sample { get; init; } = string.Empty;
        public float GSMTrail1 { get; init; }
    }

    /// <summary>
    /// 多组分-拆分成分
    /// </summary>
    public record SplittingFiberComponent : FiberComponent
    {
        public override AnalysisType Type => AnalysisType.Multiple;

        public float GSMTrail1 { get; init; }
        public float GSMTrail2 { get; init; }
        public int SplittingOrder { get; init; } // 拆分顺序
        public List<CellulosicSubFiber> CellulosicSubFibers { get; init; } = new();
    }

    /// <summary>
    /// 多组分-溶解成分
    /// </summary>
    public record DissolvedFiberComponent : FiberComponent
    {
        public override AnalysisType Type => AnalysisType.Multiple;

        public float OriginalGSMTrail1 { get; init; }
        public float OriginalGSMTrail2 { get; init; }
        public string Sample { get; init; } = string.Empty;
        public List<MultiDissolvedUnit> DissolutionUnits { get; init; } = new();
    }

    public record MultiDissolvedUnit
    {
        public string FiberName { get; init; } = string.Empty;
        public float GSMTrail1 { get; init; }
        public float GSMTrail2 { get; init; }
        public int DissolutionStep { get; init; } // 溶解步骤
        public List<CellulosicSubFiber> CellulosicSubFibers { get; init; } = new();
    }

    /// <summary>
    /// cellulosic fibre 细分纤维（仅 hemp/cotton/linen/ramie，百分比为占整体比例）
    /// </summary>
    public record CellulosicSubFiber
    {
        public string FiberName { get; init; } = string.Empty;
        public decimal Percentage { get; init; }
    }
}
