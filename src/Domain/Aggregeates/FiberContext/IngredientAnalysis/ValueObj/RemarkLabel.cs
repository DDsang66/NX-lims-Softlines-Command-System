namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.ValueObj
{
    public record RemarkLabel
    {
        public List<string> RecommendedLabel { get; set; } = new();
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
}
