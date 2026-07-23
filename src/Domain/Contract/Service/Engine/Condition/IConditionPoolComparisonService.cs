using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition
{
    public interface IConditionPoolComparisonService: IScopedDependency
    {
        /// <summary>
        /// 比较两个条件池的条件集合是否完全相等
        /// </summary>
        bool AreConditionsEqual(ConditionPool left, ConditionPool right);

        /// <summary>
        /// 比较两个条件池的条件集合是否兼容（允许某些字段差异）
        /// </summary>
        bool AreConditionsCompatible(ConditionPool left, ConditionPool right, IEnumerable<string>? ignoredFields = null);

        /// <summary>
        /// 找出两个条件池的差异
        /// </summary>
        ConditionDiff Compare(ConditionPool left, ConditionPool right);
    }
}
