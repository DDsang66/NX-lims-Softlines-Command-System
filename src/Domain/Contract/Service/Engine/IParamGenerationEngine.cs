using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine
{
    /// <summary>
    /// 引擎接口：将已加载的规则与条件池作为输入，输出 ParamSet（纯计算，无副作用）
    /// </summary>
    public interface IParamGenerationEngine:IScopedDependency
    {
        /// <summary>
        /// 根据条件池和规则生成参数集
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="rules"></param>
        /// <returns></returns>
        ParamSet Generate(ConditionPool pool, IEnumerable<ParamRule> rules);
    }
}
