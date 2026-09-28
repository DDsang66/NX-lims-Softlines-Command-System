namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.ValueObj
{
    public record RemarkLabel
    {
        public List<string> RecommendedLabel { get; set; } = new();
        public string ResultRemark { get; set; } = string.Empty;
        public string LabelRemark { get; set; } = string.Empty;
        public string JudgmentLabelRemark { get; set; } = string.Empty;
        public string LanguageLabelRemark { get; set; } = string.Empty;

        /// <summary>
        /// 模板 conclusion 段的唯一字段（书签名拼作 <c>VertifyResult</c>）。
        /// </summary>
        /// <remarks>
        /// 2026-09-28：同段的 <c>DurabilityLabel</c> / <c>OtherLabel</c> / <c>Comprehensive</c> /
        /// <c>FinalResult</c> 四个字段随模板删行一并下线，此处不再保留 ——
        /// 库里老记录的 remark JSON 里那几个键还在，但已无任何代码读取（"老报告不管"）。
        /// </remarks>
        public string VerifyResult { get; set; } = string.Empty;
    }
}
