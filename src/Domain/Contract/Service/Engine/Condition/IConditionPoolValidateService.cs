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
        /// 验证二级条件池是否满足结构公式要求
        /// </summary>
        /// <param name="structure"></param>
        /// <param name="pool"></param>
        /// <returns></returns>
        Task<Result> ValidateConditionPool(ParamStructure structure, Formula formula, ConditionPool pool);
    }
}
