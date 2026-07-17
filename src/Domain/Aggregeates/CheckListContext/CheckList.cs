using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext
{
    public sealed class CheckList: AggregateRoot<CheckListId,Guid>
    {
        private readonly List<OrderId?> _orderIds = new();

        /// <summary>
        /// 关联申请单Id
        /// </summary>
        public IReadOnlyCollection<OrderId?> OderIds => _orderIds.AsReadOnly();

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
        /// 创建测试清单
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="items"></param>
        /// <param name="remark"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static CheckList Create(
            IEnumerable<OrderId?> orderIds,
            IReadOnlyList<CheckListItem> items,
            string? remark)
        {
            var id = new CheckListId(Guid.NewGuid());

            if (items == null || items.Count == 0)
                throw new ArgumentNullException("items");

             var c = new CheckList
             {
                 Id = id,
                 Items = items,
                 Remark = remark
             };

            if (orderIds != null)
            {
                foreach (var orderId in orderIds.Where(oid => oid != null))
                {
                    c._orderIds.Add(orderId);
                }
            }

            return c;
        }
    }
}
