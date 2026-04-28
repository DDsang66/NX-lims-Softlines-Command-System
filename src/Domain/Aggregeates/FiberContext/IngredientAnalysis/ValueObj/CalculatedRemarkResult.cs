namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.ValueObj
{
    /// <summary>
    /// 强类型备注（值对象基类）
    /// </summary>
    public record CalculatedRemarkResult:RemarkLabel
    {
        public List<string> Results { get; init; } = new();
        public List<string> Recommendation { get; init; } = new();
    }
}
