using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj
{
    /// <summary>
    /// 订单标识值对象（即 ReportNumber，如 "87.405.26.0001.01"）
    /// </summary>
    public sealed class OrderId:AggregateRootId<Guid>
    {
        public OrderId(Guid value)
            :base(value)
        {
            if (value == Guid.Empty)
                throw new ArgumentNullException("OrderId cannot be empty", nameof(value));
        }
    }
}
