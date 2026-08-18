using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine
{
    public interface IParamEngineScheduler : IScopedDependency
    {
        /// <summary>
        /// 为指定测试项目与标准集合收集公式、结构与规则。
        /// 返回包含公式、参数结构与规则的聚合结果。
        /// </summary>
        /// <param name="testItemId"></param>
        /// <param name="standardIds"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<ParamEngineScheduleResult> CollectForTestItemAsync(
            TestItemId testItemId,
            IEnumerable<StandardId> standardIds,
            CancellationToken cancellationToken);
    }
}
