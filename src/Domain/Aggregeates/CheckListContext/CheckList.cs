using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext
{
    public sealed class CheckList: AggregateRoot<CheckListId,Guid>
    {
        /// <summary>
        /// 关联申请单Id
        /// </summary>
        public OrderId? OderId { get; private set; }

        /// <summary>
        /// 测试清单中的测试项
        /// </summary>
        public IReadOnlyList<CheckListItem> Items { get; private set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedTime { get; private set; } = DateTime.Now;

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; private set; } = string.Empty;

        /// <summary>
        /// 测试清单状态
        /// </summary>
        public CheckListStatus Status { get; private set; } = CheckListStatus.Created;

        /// <summary>
        /// 创建测试清单
        /// </summary>
        /// <param name="orderIds"></param>
        /// <param name="items"></param>
        /// <param name="remark"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static CheckList Create(
            OrderId? orderId,
            IReadOnlyList<CheckListItem> items,
            string? remark)
        {
            var id = new CheckListId(Guid.NewGuid());

            if (items == null || items.Count == 0)
                throw new ArgumentNullException("items");

            foreach (var item in items)
            {
                item.BindToCheckList(id);
            }

            var c = new CheckList
             {
                 Id = id,
                 Items = items,
                 Status = CheckListStatus.Created,
                 CreatedTime = DateTime.Now,
                 Remark = remark
             };

            if (orderId != null)
            {
                c.OderId = orderId;
            }

            return c;
        }

        /// <summary>
        /// 重建测试清单
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orderIds"></param>
        /// <param name="items"></param>
        /// <param name="CreatedTime"></param>
        /// <param name="remark"></param>
        /// <returns></returns>
        public static CheckList Reconstitute(
            CheckListId id,
            OrderId orderId,
            IReadOnlyList<CheckListItem> items,
            DateTime CreatedTime,
            CheckListStatus status,
            string? remark) 
        {
            var c = new CheckList
            {
                Id = id,
                Items = items,
                Remark = remark,
                Status = status,
                CreatedTime = CreatedTime,
            };

            if (orderId != null)
            {
                c.OderId = orderId;
            }

            return c;
        }

        /// <summary>
        /// 更新测试清单（更新备注及测试项）
        /// </summary>
        /// <param name="newItems">新的测试项集合</param>
        /// <param name="remark">新的备注</param>
        /// <exception cref="InvalidOperationException">当清单状态不允许更新时抛出</exception>
        /// <exception cref="ArgumentNullException">当测试项为空时抛出</exception>
        public void Update(IReadOnlyList<CheckListItem> newItems, string? remark)
        {
            // 1. 状态守卫：已完成或进行中的清单通常不允许被随意更新
            if (Status == CheckListStatus.Completed || Status == CheckListStatus.InProgress)
            {
                throw new InvalidOperationException($"当前清单状态为 {Status}，不允许更新！");
            }

            // 2. 业务规则校验：测试项不能为空
            if (newItems == null || newItems.Count == 0)
                throw new ArgumentNullException(nameof(newItems), "测试清单至少需要包含一个测试项");

            // 3. 更新属性，保持聚合根内部一致性
            Remark = remark;

            // 4. 维护聚合根与内部实体的关联关系
            foreach (var item in newItems)
            {
                item.BindToCheckList(Id); // 确保所有的测试项都归属于当前清单
            }
            Items = newItems;

            // 5. (可选) 如果领域事件存在，可以触发清单更新事件
            // AddDomainEvent(new CheckListUpdatedEvent(Id));
        }


        /// <summary>
        /// 更新测试项的参数计算结果（领域行为）
        /// </summary>
        /// <param name="itemParamsDict">Key: CheckListItem的Id, Value: 该Item的测点参数字典</param>
        public void UpdateItemParameters(Dictionary<Guid, IReadOnlyDictionary<string, ParamSet?>> itemParamsDict)
        {
            // 1. 状态守卫
            if (Status == CheckListStatus.Completed)
            {
                throw new InvalidOperationException("已完成的清单不允许重新计算/更新参数");
            }

            // 2. 遍历当前清单的测试项，将计算结果更新进去
            foreach (var item in Items)
            {
                if (itemParamsDict.TryGetValue(item.Id, out var paramsForItem))
                {
                    // 调用内部实体的领域方法
                    item.UpdateTestPointParams(paramsForItem);
                }
            }

            // 状态流转：参数计算完成，清单可以进入进行中状态（根据你的业务逻辑调整）
            // ChangeInProcess(); 
        }

        /// <summary>
        /// 删除
        /// </summary>
        public void Delete() { }

        /// <summary>
        /// 审单完成
        /// </summary>
        public void ReviewFinish() 
        {
            //add domain event
        }

        /// <summary>
        /// 暴露测试项目
        /// </summary>
        public IReadOnlyCollection<CheckListItem> GetTestItem() 
        {
            if (Items == null || Items.Count == 0) return null;

            return Items;
        }

        /// <summary>
        /// 更改状态为进行中
        /// </summary>
        public void ChangeInProcess() 
        {
            Status = CheckListStatus.InProgress;
        }

        /// <summary>
        /// 更改状态为已完成
        /// </summary>
        public void ChangeToCompleted()
        {
            Status = CheckListStatus.Completed;
        }

    }
}
