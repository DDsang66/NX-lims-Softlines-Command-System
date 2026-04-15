namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs
{
    public record BuildAnalysisDto
    {
        public string ReportNumber { get; init; } = string.Empty;
        public List<string> Method { get; init; } = new();
        public string ComponentType { get; init; } = string.Empty;
        public string Buyer { get; init; } = string.Empty;
        public MultipleAnalysis MultipleBuildAnalysis { get; init; } = new();
        public SingleAnalysis SingleBuildAnalysis { get; init; } = new();
        public List<string> RecommendedLabel { get; init; } =new();
        public string ResultRemark { get; init; } = string.Empty;
        public string LabelRemark { get; init; } = string.Empty;
        public string JudgmentLabelRemark { get; init; } = string.Empty;
        public string LanguageLabelRemark { get; init; } = string.Empty;
        public string DurabilityLabel { get; init; } = string.Empty;
        public string OtherLabel { get; init; } = string.Empty;
        public string Comprehensive { get; init; } = string.Empty;
        public string VerifyResult { get; init; } = string.Empty;
        public string FinalResult { get; init; } = string.Empty;
    }

    /// <summary>
    /// 单组分表单
    /// </summary>
    public record SingleAnalysis 
    {
        public List<SingleFiberRow> SingleFiberRows { get; init; } = new();
    }

    /// <summary>
    /// 多组分表单
    /// </summary>
    public record MultipleAnalysis 
    {
        public List<FiberSplittingList> fiberSplittingList { get; init; } = new();
        public List<FiberDissolvedList> fiberDissolvedList { get; init; } = new();
    }

    /// <summary>
    /// 拆分列表
    /// </summary>
    public record FiberSplittingList
    {
        public List<SplittingRow> SplittingRows { get; init; } = new();
    }

    /// <summary>
    /// 溶解列表
    /// </summary>
    public record FiberDissolvedList
    {
        public float OriginalGSMTrail1 { get; init; } = 0;
        public float OriginalGSMTrail2 { get; init; } = 0;
        public List<DissolvedRow> DissolvedRows { get; init; } = new();
    }

    /// <summary>
    /// 溶解行
    /// </summary>
    public record DissolvedRow
    {
        public string FiberName { get; init; } = string.Empty;
        public float GSMTrail1 { get; init; } = 0;
        public float GSMTrail2 { get; init; } = 0;
    }

    /// <summary>
    /// 拆分行
    /// </summary>
    public record SplittingRow
    {
        public string FiberName { get; init; } = string.Empty;
        public float GSMTrail1 { get; init; } = 0;
        public float GSMTrail2 { get; init; } = 0;
    }

    /// <summary>
    /// 单组分行
    /// </summary>
    public record SingleFiberRow
    {
        public string Sample { get; init; } = string.Empty;
        public string FiberName { get; init; } = string.Empty;
        public float GSMTrail1 { get; init; } = 0;
    }




}
