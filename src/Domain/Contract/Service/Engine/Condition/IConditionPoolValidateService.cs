using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition
{
    /// <summary>
    /// 条件池验证服务服务集
    /// </summary>
    public interface IConditionPoolValidateService:IScopedDependency
    {
        /// <summary>
        /// 验证一级条件池是否满足结构要求（结构层面）
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="pool"></param>
        /// <returns></returns>
        Task<Result> EnsureConditionPoolConformance(ParamStructure structure, ConditionPool pool);

        /// <summary>
        /// 验证二级条件池是否满足公式要求（公式层面）
        /// </summary>
        /// <param name="formula"></param>
        /// <param name="pool"></param>
        /// <returns></returns>
        Task<Result> EnsureConditionPoolWithFormula(Formula formula, ConditionPool pool);
    }
}
