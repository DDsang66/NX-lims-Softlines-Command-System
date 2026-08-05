using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.OrderContext;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.OrderContext
{
    /// <summary>
    /// 订单查询服务接口（CQRS 读模型，定义在 Application 层，实现在 Infrastructure 层）
    /// </summary>
    public interface IOrderQueryService
    {
        Task<OrderOutput[]> GetOrderListAsync(string userId);
        Task<object> GetOrderSummaryAsync(OrderQueryParams dto);
        Task<OrderCardOutput> GetOrderCardListAsync(DateTimeOffset time, string group, string timeType);
        Task<OrderFanCardOutput> GetOrderFanChartListAsync(DateTimeOffset time, string group, string timeType);
        Task<OrderLineCardOutput> GetOrderLineChartAsync(DateTimeOffset[] time, string group, string timeType, string Type);
    }
}
