namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Util
{
    /// <summary>
    /// 公式解析上下文（值对象）
    /// 
    /// 从 Formula 聚合根提取，只包含 Parser 需要的信息
    /// </summary>
    public sealed class FormulaParseContext
    {
        /// <summary>
        /// 公式模板（用于判断槽位语法）
        /// </summary>
        public string ExpressionTemplate { get; }

        /// <summary>
        /// 条件字段列表（旧式语法时使用）
        /// </summary>
        public IReadOnlyList<string> ConditionFields { get; }

        /// <summary>
        /// 公式名称（仅用于错误信息）
        /// </summary>
        public string FormulaName { get; }

        public FormulaParseContext(
            string expressionTemplate,
            IReadOnlyList<string> conditionFields,
            string formulaName)
        {
            ExpressionTemplate = expressionTemplate ?? string.Empty;
            ConditionFields = conditionFields ?? Array.Empty<string>();
            FormulaName = formulaName ?? string.Empty;
        }
    }
}
