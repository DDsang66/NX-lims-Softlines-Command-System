using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj
{
    /// <summary>
    /// 订单标识 = 报告号（字符串业务主键）
    /// </summary>
    public sealed class OrderId : AggregateRootId<string>
    {
        public OrderId(string reportNumber)
            : base(reportNumber)
        {
            if (string.IsNullOrWhiteSpace(reportNumber))
                throw new ArgumentException("OrderId (ReportNumber) cannot be empty", nameof(reportNumber));
        }
    }
}
