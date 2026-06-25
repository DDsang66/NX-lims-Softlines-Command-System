using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition
{
    /// <summary>
    /// 用于构建条件模式的接口
    /// </summary>
    public interface IConditionPatternBuilder:IScopedDependency
    {
        IConditionPatternBuilder AddEqual(string field, object? value);
        IConditionPatternBuilder AddComparison(string fieldPath, ComparisonOperator op, object? value);
        IConditionPatternBuilder AddIn(string field, IEnumerable<object?> values);
        IConditionPatternBuilder AddComposite(CompositeCondition composite);
        ConditionPattern Build();
    }
}
