namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.IngredientAnalysis.ValueObj
{
    /// <summary>
    /// 成分分析结果（值对象）
    /// </summary>
    public record AnalysisResult
    {
        /// <summary>
        /// 字典集合，用于Word文档映射
        /// </summary>
        public Dictionary<string, object> Data { get; init; } = new();

        public AnalysisResult() { }

        public AnalysisResult(Dictionary<string, object> data)
        {
            Data = data;
        }

        // 便捷构造
        public static AnalysisResult From(Dictionary<string, object> data) => new(data);
        public static AnalysisResult Empty() => new(new Dictionary<string, object>());

        // 读取
        public T? Get<T>(string key) =>
            Data.TryGetValue(key, out var value) ? (T?)value : default;

        public object? this[string key] =>
            Data.TryGetValue(key, out var value) ? value : null;
    }

}
