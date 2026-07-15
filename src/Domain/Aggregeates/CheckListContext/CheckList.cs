using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext
{
    public sealed class CheckList: AggregateRoot
    {
        /// <summary>
        /// 测试清单ID
        /// </summary>
        public CheckListId Id { get; private set; }

        /// <summary>
        /// 关联申请单Id
        /// </summary>
        public OrderId SourceId { get; private set; } // 关联的申请单ID
        
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

        private CheckList() { }

        /// <summary>
        /// 创建测试清单
        /// </summary>
        /// <param name="sourceId"></param>
        /// <param name="items"></param>
        /// <param name="remark"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static CheckList Create(
            OrderId sourceId,
            IReadOnlyList<CheckListItem> items,
            string? remark)
        {
            var id = new CheckListId(Guid.NewGuid());

            if (items == null || items.Count == 0)
                throw new ArgumentNullException("items");

            if (sourceId == null)
                throw new ArgumentNullException("source");
            return new CheckList
            {
                Id = id,
                SourceId = sourceId,
                Items = items,
                Remark = remark
            };
        }
    }
}
