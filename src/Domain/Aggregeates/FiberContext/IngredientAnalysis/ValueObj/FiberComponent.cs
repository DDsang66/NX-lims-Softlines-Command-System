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
        // 单组分**不称量**（B13，2026-09-28）：GSMTrail1 已随前端的 Gradient GSM 列一并退役。
        // 它从来没进过任何模板书签（单分支写的 GSMTrail1_{idx} 两份模板都没有），
        // 单组分的 Rate 也是硬编码 100%。**别按"和多组分对齐"再加回来。**
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
        public List<BicomponentSubFiber> BicomponentSubFibers { get; init; } = new();
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
        public List<BicomponentSubFiber> BicomponentSubFibers { get; init; } = new();
    }

    /// <summary>
    /// cellulosic fibre 细分纤维（仅 hemp/cotton/linen/ramie，百分比为占整体比例）
    /// </summary>
    public record CellulosicSubFiber
    {
        public string FiberName { get; init; } = string.Empty;
        public decimal Percentage { get; init; }
    }

    /// <summary>
    /// Bicomponent/Biconstituent 双组分子纤维（固定的 Polyester + Polyamide，克重输入）
    /// </summary>
    public record BicomponentSubFiber
    {
        public string FiberName { get; init; } = string.Empty;
        public decimal GSMTrail1 { get; init; }
        public decimal GSMTrail2 { get; init; }
    }
}
