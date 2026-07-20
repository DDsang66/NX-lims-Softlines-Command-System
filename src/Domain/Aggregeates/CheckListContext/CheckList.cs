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
                item.CheckListId = id;
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
        /// 更新测试清单
        /// </summary>
        public void Update() { }

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
