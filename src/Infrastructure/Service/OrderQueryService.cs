using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.OrderContext;
using NX_lims_Softlines_Command_System.Infrastructure.Data.Repositories.OrderRepos;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Application.Interface.OrderContext;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Service
{
    /// <summary>
    /// 订单查询服务 — 封装旧 OrderRepo 的复杂查询逻辑
    /// 通过 IOrderQueryService 接口提供，AppService 依赖接口而非具体类
    /// </summary>
    public class OrderQueryService : IOrderQueryService, IScopedDependency
    {
        private readonly OrderRepo _repo;

        public OrderQueryService(OrderRepo repo)
        {
            _repo = repo;
        }

        public async Task<OrderOutput[]> GetOrderListAsync(string userId)
            => await _repo.GetOrderListAsync(userId);

        public async Task<object> GetOrderSummaryAsync(OrderQueryParams dto)
            => await _repo.GetSummaryOrdersAsync(dto);

        public async Task<OrderCardOutput> GetOrderCardListAsync(DateTimeOffset time, string group, string timeType)
        {
            var utcTime = time.ToUniversalTime().ToOffset(TimeSpan.FromHours(8));
            return await _repo.OrderCardAsync(utcTime, group, timeType);
        }

        public async Task<OrderFanCardOutput> GetOrderFanChartListAsync(DateTimeOffset time, string group, string timeType)
        {
            var utcTime = time.ToUniversalTime().ToOffset(TimeSpan.FromHours(8));
            return await _repo.OrderfanCardAsync(utcTime, group, timeType);
        }

        public async Task<OrderLineCardOutput> GetOrderLineChartAsync(DateTimeOffset[] time, string group, string timeType, string Type)
        {
            var utcTimeArray = time.Select(t => t.ToUniversalTime().ToOffset(TimeSpan.FromHours(8))).ToArray();
            return await _repo.OrderLineChartAsync(utcTimeArray, group, timeType, Type);
        }
    }
}
