using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine
{
    public interface IParamRuleValidateService:IScopedDependency
    {
        /// <summary>
        /// 按顺序执行原子校验（保留以兼容历史调用，推荐应用层自行编排）
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="formula"></param>
        /// <param name="structure"></param>
        /// <returns></returns>
        Result Validate(ParamRule rule, Formula formula, ParamStructure? structure = null);
    }
}
