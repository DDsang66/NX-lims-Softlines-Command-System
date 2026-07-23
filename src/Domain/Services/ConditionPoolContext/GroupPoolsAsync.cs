using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Services.ConditionPoolContext
{
    public class GroupPoolsAsync:IGroupPoolsAsync, IScopedDependency
    {
        /// <summary>
        /// 根据条件分组
        /// </summary>
        /// <param name="originalPool"></param>
        /// <param name="groupData"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public (ConditionPool updatedPool, List<ConditionPool> newPools) Group(
                ConditionPool originalPool,
                List<(Dictionary<string, object?> Conditions, List<string> TestPoints)> groupData)
        {
            if (originalPool == null)
                throw new ArgumentNullException(nameof(originalPool));

            if (groupData == null || groupData.Count == 0)
                throw new ArgumentException("分组数据不能为空", nameof(groupData));

            var newPools = new List<ConditionPool>();

            // 处理第一个：合并到原始池
            var firstItem = groupData.First();
            originalPool.MergeFrom(firstItem.Conditions, firstItem.TestPoints);

            // 处理后续的：生成新池
            foreach (var item in groupData.Skip(1))
            {
                var newPool = ConditionPool.Create(originalPool.CheckListId, item.Conditions);
                newPool.AddTestPoints(item.TestPoints);
                newPools.Add(newPool);
            }

            return (originalPool, newPools);
        }
    }
}
