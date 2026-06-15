namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs
{
    public class BuildAnalysisDto
    {
        public string ReportNumber { get; set; } = string.Empty;
        public List<string> Method { get; set; } = new();
        public string ComponentType { get; set; } = string.Empty;
        public string Buyer { get; set; } = string.Empty;
        public MultipleAnalysis MultipleBuildAnalysis { get; set; } = new();
        public SingleAnalysis SingleBuildAnalysis { get; set; } = new();
        public List<string> RecommendedLabel { get; set; } =new();
        public string ResultRemark { get; set; } = string.Empty;
        public string LabelRemark { get; set; } = string.Empty;
        public string JudgmentLabelRemark { get; set; } = string.Empty;
        public string LanguageLabelRemark { get; set; } = string.Empty;
        public string DurabilityLabel { get; set; } = string.Empty;
        public string OtherLabel { get; set; } = string.Empty;
        public string Comprehensive { get; set; } = string.Empty;
        public string VerifyResult { get; set; } = string.Empty;
        public string FinalResult { get; set; } = string.Empty;
    }

    /// <summary>
    /// 单组分表单
    /// </summary>
    public record SingleAnalysis 
    {
        public List<SingleFiberRow> SingleFiberRows { get; set; } = new();
    }

    /// <summary>
    /// 多组分表单
    /// </summary>
    public record MultipleAnalysis 
    {
        public List<FiberSplittingList> fiberSplittingList { get; set; } = new();
        public List<FiberDissolvedList> fiberDissolvedList { get; set; } = new();
    }

    /// <summary>
    /// 拆分列表
    /// </summary>
    public record FiberSplittingList
    {
        public List<SplittingRow> SplittingRows { get; set; } = new();
    }

    /// <summary>
    /// 溶解列表
    /// </summary>
    public record FiberDissolvedList
    {
        public float OriginalGSMTrail1 { get; set; } = 0;
        public float OriginalGSMTrail2 { get; set; } = 0;
        public List<DissolvedRow> DissolvedRows { get; set; } = new();
    }

    /// <summary>
    /// 溶解行
    /// </summary>
    public record DissolvedRow
    {
        public string FiberName { get; set; } = string.Empty;
        public float GSMTrail1 { get; set; } = 0;
        public float GSMTrail2 { get; set; } = 0;
    }

    /// <summary>
    /// 拆分行
    /// </summary>
    public record SplittingRow
    {
        public string FiberName { get; set; } = string.Empty;
        public float GSMTrail1 { get; set; } = 0;
        public float GSMTrail2 { get; set; } = 0;
    }

    /// <summary>
    /// 单组分行
    /// </summary>
    public record SingleFiberRow
    {
        public string Sample { get; set; } = string.Empty;
        public string FiberName { get; set; } = string.Empty;
        public float GSMTrail1 { get; set; } = 0;
    }
}
