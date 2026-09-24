using System.ComponentModel.DataAnnotations;

namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs
{
    public class BuildAnalysisDto
    {
        /// <summary>落库列 fiber_analysis.report_number = varchar(20)。</summary>
        [StringLength(20)]
        public string ReportNumber { get; set; } = string.Empty;

        /// <summary>
        /// 多选标准。落库时 string.Join(",", Method) 写进 varchar(255)。
        /// </summary>
        /// <remarks>
        /// **刻意不加 [StringLength]**：该特性只接受 string，套在这个字符串列表上
        /// 每次校验都会抛 <see cref="InvalidCastException"/>（即每个请求 500）。
        /// 而 [MaxLength] 套在集合上校验的是**元素个数**、不是拼接后的字符数，也不是想要的口径。
        /// 现有生产值最长为 AATCC TM20-2021,ISO1833（25 字符），远未触顶。
        /// </remarks>
        public List<string> Method { get; set; } = new();

        public string ComponentType { get; set; } = string.Empty;

        /// <summary>落库列 fiber_analysis.buyer = varchar(20)。</summary>
        [StringLength(20)]
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
        public string Sample { get; set; } = string.Empty;
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
        public List<CellulosicSubFiberDto> CellulosicSubFibers { get; set; } = new();
        public List<BicomponentSubFiberDto> BicomponentSubFibers { get; set; } = new();
    }

    /// <summary>
    /// 拆分行
    /// </summary>
    public record SplittingRow
    {
        public string FiberName { get; set; } = string.Empty;
        public float GSMTrail1 { get; set; } = 0;
        public float GSMTrail2 { get; set; } = 0;
        public List<CellulosicSubFiberDto> CellulosicSubFibers { get; set; } = new();
        public List<BicomponentSubFiberDto> BicomponentSubFibers { get; set; } = new();
    }

    /// <summary>
    /// cellulosic 子纤维 DTO
    /// </summary>
    public record CellulosicSubFiberDto
    {
        public string FiberName { get; set; } = string.Empty;
        public decimal Percentage { get; set; }
    }

    /// <summary>
    /// Bicomponent/Biconstituent 子纤维 DTO（克重输入，不是百分比）
    /// </summary>
    public record BicomponentSubFiberDto
    {
        public string FiberName { get; set; } = string.Empty;
        public decimal GSMTrail1 { get; set; }
        public decimal GSMTrail2 { get; set; }
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
