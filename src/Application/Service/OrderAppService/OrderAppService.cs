using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.AuthenticationService;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Application.Contract;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.OrderContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.OrderAppService
{
    public class OrderAppService : IScopedDependency
    {
        private readonly IOrderRepository _repository;
        private readonly IOrderQueryService _queryService;
        private readonly IOrderLookupService _lookup;

        public OrderAppService(IOrderRepository repository, IOrderQueryService queryService, IOrderLookupService lookup)
        {
            _repository = repository;
            _queryService = queryService;
            _lookup = lookup;
        }

        /* ================================================================
         * 读 — 查询
         * ================================================================ */

        public async Task<OrderOutput[]> GetOrderListAsync(string userId)
            => await _queryService.GetOrderListAsync(userId);

        public async Task<object> GetOrderSummaryAsync(OrderQueryParams dto)
            => await _queryService.GetOrderSummaryAsync(dto);

        public async Task<OrderCardOutput> GetOrderCardListAsync(DateTimeOffset time, string group, string timeType)
            => await _queryService.GetOrderCardListAsync(time, group, timeType);

        public async Task<OrderFanCardOutput> GetOrderFanChartListAsync(DateTimeOffset time, string group, string timeType)
            => await _queryService.GetOrderFanChartListAsync(time, group, timeType);

        public async Task<OrderLineCardOutput> GetOrderLineChartAsync(DateTimeOffset[] time, string group, string timeType, string Type)
            => await _queryService.GetOrderLineChartAsync(time, group, timeType, Type);

        /// <summary>
        /// 新增订单 — 成功返回 true，失败返回 false
        /// </summary>
        public async Task<bool> AddOrderAsync(OrderDto dto)
        {
            if (dto?.Rows == null || dto.Rows.Count == 0) return false;

            try
            {
                var firstRow = dto.Rows.First();
                if (string.IsNullOrWhiteSpace(firstRow.ReportNum) || firstRow.OrderEntry == null)
                    return false;

                var cs = dto.Rows[0].Cs;
                var csName = await _lookup.ResolveCsNameAsync(cs);
                var order = Order.Create(firstRow.ReportNum, firstRow.OrderEntry, csName, dto.Remark);

                foreach (var row in dto.Rows)
                {
                    if (await _repository.ExistsAsync(order.Id, row.Group!, CancellationToken.None))
                        return false;

                    order.AddLine(
                        lineId: new SnowflakeIdGenerator().NextId(),
                        testGroup: row.Group!,
                        express: StringToExpress(row.Express),
                        dueDate: row.DueDate ?? DateTimeOffset.UtcNow,
                        labIn: row.LabIn ?? DateTimeOffset.UtcNow,
                        remark: row.Remark);
                }

                await _repository.AddAsync(order, CancellationToken.None);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 更新订单行 — 成功返回 true，失败返回 false
        /// </summary>
        public async Task<bool> UpdateOrderAsync(OrderUpdateDto dto)
        {
            if (dto?.Rows == null || dto.Rows.Count == 0) return false;

            try
            {
                foreach (var row in dto.Rows)
                {
                    if (row.RecordId == null || !long.TryParse(row.RecordId, out var lineId))
                        continue;

                    var orderId = await _repository.GetOrderIdByLineIdAsync(lineId, CancellationToken.None);
                    if (orderId == null) continue;

                    var order = await _repository.GetByIdAsync(new OrderId(orderId.Value), CancellationToken.None);
                    if (order == null) continue;

                    order.UpdateLine(lineId,
                        express: StringToExpress(row.Express),
                        dueDate: row.ReportDueDate,
                        labIn: row.OrderInTime,
                        sampleCount: row.TestSampleNum,
                        itemCount: row.TestItemNum,
                        reviewer: await ResolveUserName(row.ReviewerId),
                        engineer: row.TestEngineer,
                        remark: row.Remark,
                        delayType: row.DelayType,
                        delayReason: row.DelayReason);

                    order.ApplyTimeBasedStatusTransition(lineId,
                        reviewer: await ResolveUserName(row.ReviewerId),
                        engineer: row.TestEngineer,
                        reviewFinishTime: row.ReviewFinishTime,
                        labOutTime: row.LabOutTime);

                    await _repository.UpdateAsync(order, CancellationToken.None);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 软删除订单行 — 遍历 OrderDeleteRequest.Items，逐行删除
        /// </summary>
        public async Task<bool> DeleteOrderAsync(OrderDeleteRequest req)
        {
            if (req?.Items == null || req.Items.Count == 0) return false;

            try
            {
                foreach (var item in req.Items)
                {
                    if (!long.TryParse(item.RecordId, out var recordId)) continue;

                    var orderId = await _repository.GetOrderIdByLineIdAsync(recordId, CancellationToken.None);
                    if (orderId == null) continue;

                    var order = await _repository.GetByIdAsync(new OrderId(orderId.Value), CancellationToken.None);
                    if (order == null) continue;

                    order.DeleteLine(recordId);
                    await _repository.UpdateAsync(order, CancellationToken.None);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<Order?> GetOrderByIdAsync(Guid orderId)
        {
            return await _repository.GetByIdAsync(new OrderId(orderId), CancellationToken.None);
        }

        private static OrderExpress StringToExpress(string? s) => s switch
        {
            "Same Day" => OrderExpress.SameDay,
            "Shuttle" => OrderExpress.Shuttle,
            "Express" => OrderExpress.Express,
            _ => OrderExpress.Regular
        };

        private async Task<string?> ResolveUserName(string? userId)
        {
            return await _lookup.ResolveUserNameAsync(userId);
        }
    }
}
