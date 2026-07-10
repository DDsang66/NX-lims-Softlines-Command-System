using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext
{
    public sealed class CheckList: IAggregateRoot
    {
        public CheckListId Id { get; set; }
        public OrderId SourceId { get; set; } = string.Empty;  // 关联的申请单ID
        public IReadOnlyList<CheckListItem> Items { get; set; }
        public string Remark { get; set; } = string.Empty;

        public CheckList() { }
    }
}
