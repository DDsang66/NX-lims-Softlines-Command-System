using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition
{
    public interface IConditionPoolDomainService:IScopedDependency
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
        (ConditionPool updatedPool, List<ConditionPool> poolsToUpdate, List<ConditionPool> poolsToCreate, List<ConditionPool> poolsToDelete) GroupWithReuse(
                    ConditionPool originalPool,
                    List<ConditionPool> existingPools,
                    List<(Dictionary<string, object?> Conditions, List<string> TestPoints)> groupData);
        
        /// <summary>
        /// 构建必填条件
        /// </summary>
        /// <param name="paramStructures"></param>
        /// <returns></returns>
        IDictionary<string, object?> GenerateRequiredConditions(IEnumerable<ParamStructure> paramStructures);

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

        /// <summary>
        /// 尝试从 ConditionPool 中按路径取值（支持嵌套路径 "A.B.C"）
        /// 返回 false 表示不存在或无法访问。
        /// </summary>
        bool TryGet(ConditionPool pool, string path, out object? value);

        /// <summary>
        /// 将原始（一级）数据丰富为二级原子条件，并返回新的 ConditionPool
        /// </summary>
        ConditionPool Enrich(IDictionary<string, object?> rawData);
    }
}
