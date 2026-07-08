using System.Text.Json.Nodes;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Util
{
    /// <summary>
    /// 解析结果
    /// </summary>
    public sealed class ParsedRule
    {
        /// <summary>
        /// 条件模式 JSON（符合 ConditionPattern 格式）
        /// </summary>
        public JsonObject ConditionPatternJson { get; set; }

        /// <summary>
        /// 推导符右边的结果值
        /// </summary>
        public string ResultValue { get; set; }

        /// <summary>
        /// 原始规则文本（用于追溯）
        /// </summary>
        public string SourceText { get; set; }
    }
}
