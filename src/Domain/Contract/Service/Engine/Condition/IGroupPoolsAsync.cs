using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition
{
    public interface IGroupPoolsAsync:IScopedDependency
    {
        /// <summary>
        /// 分组条件池（纯内存操作，不依赖仓储）
        /// </summary>
        /// <param name="originalPool">原始条件池（由应用层查询传入）</param>
        /// <param name="groupData">分组数据</param>
        /// <returns>
        /// 返回操作结果：
        /// - updatedPool: 更新后的原始池
        /// - newPools: 需要新建的条件池列表
        /// </returns>
        (ConditionPool updatedPool, List<ConditionPool> newPools) Group(
            ConditionPool originalPool,
            List<(Dictionary<string, object?> Conditions, List<string> TestPoints)> groupData);
    }
}
