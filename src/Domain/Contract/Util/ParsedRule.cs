using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
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
        public ConditionPattern Pattern { get; set; } =new ConditionPattern();

        /// <summary>
        /// 推导符右边的结果值
        /// </summary>
        public ParamValue? Result { get; set; } = new ParamValue();

        /// <summary>
        /// 原始规则文本（用于追溯）
        /// </summary>
        public string SourceText { get; set; } = string.Empty;
    }
}
