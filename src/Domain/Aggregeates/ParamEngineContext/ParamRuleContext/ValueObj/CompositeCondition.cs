using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.Enums;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj
{

    /// <summary>
    /// 复合条件：支持简单的逻辑组合。
    /// - FieldNames: 引用已在 Equal/ In / Comparison 中声明的字段（便于写编辑器配置）
    /// - SubConditions: 可直接包含比较子条件（更灵活）
    /// </summary>
    public class CompositeCondition
    {
        public LogicalOperator Logic { get; set; } = LogicalOperator.And;

        /// <summary>
        /// 直接引用的字段名（与 EqualMatches/ InMatches/ ComparisonMatches 中的字段名对应）
        /// </summary>
        public List<string>? FieldNames { get; set; }

        /// <summary>
        /// 直接嵌套的比较子条件（用于在一个复合节点内表达复杂比较）
        /// </summary>
        public List<ComparisonCondition>? SubConditions { get; set; }
    }
}
