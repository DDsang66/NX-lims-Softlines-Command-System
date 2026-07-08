using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using System.Text.Json.Nodes;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Interface
{
    /// <summary>
    /// ConditionPattern 序列化器契约
    /// 负责将各种条件类型序列化为 JSON 结构
    /// </summary>
    public interface IConditionPatternSerializer:IScopedDependency
    {
        /// <summary>
        /// 序列化等值条件
        /// </summary>
        JsonObject SerializeEqual(string field, object? value);

        /// <summary>
        /// 序列化比较条件
        /// </summary>
        JsonObject SerializeComparison(string fieldPath, ComparisonOperator op, object? value);

        /// <summary>
        /// 序列化集合条件
        /// </summary>
        JsonObject SerializeIn(string field, IEnumerable<object?> values);

        /// <summary>
        /// 序列化复合条件
        /// </summary>
        JsonObject SerializeComposite(CompositeCondition composite);

        /// <summary>
        /// 组装完整的 ConditionPattern JSON
        /// </summary>
        JsonObject BuildPattern(
            IEnumerable<(string field, object? value)>? equals = null,
            IEnumerable<(string fieldPath, ComparisonOperator op, object? value)>? comparisons = null,
            IEnumerable<(string field, IEnumerable<object?> values)>? ins = null,
            IEnumerable<CompositeCondition>? composites = null);
    }
}
