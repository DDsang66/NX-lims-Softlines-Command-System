using Microsoft.Extensions.Logging;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.OrderContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.OrderContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Application.Interface.OrderContext;

namespace NX_lims_Softlines_Command_System.src.Application.Service.OrderAppService
{
    public class OrderAppService : IScopedDependency
    {
        private readonly IOrderRepository _repository;
        private readonly IOrderQueryService _queryService;
        private readonly IOrderLookupService _lookup;
        private readonly IWorkdayCalculator _workdayCalculator;
        private readonly ILogger<OrderAppService> _logger;

        public OrderAppService(IOrderRepository repository, IOrderQueryService queryService, IOrderLookupService lookup, IWorkdayCalculator workdayCalculator, ILogger<OrderAppService> logger)
        {
            _repository = repository;
            _queryService = queryService;
            _lookup = lookup;
            _workdayCalculator = workdayCalculator;
            _logger = logger;
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
        public async Task<bool> AddOrderAsync(AddOrderRequest dto)
        {
            if (dto?.Rows == null || dto.Rows.Count == 0)
            { _logger.LogWarning("AddOrder failed: Rows is null or empty"); return false; }

            try
            {
                var firstRow = dto.Rows.First();
                if (string.IsNullOrWhiteSpace(firstRow.ReportNumber) || firstRow.OrderEntryPerson == null)
                { _logger.LogWarning("AddOrder failed: ReportNumber={ReportNumber}, OrderEntry={OrderEntry}", firstRow.ReportNumber, firstRow.OrderEntryPerson); return false; }

                var csName = await _lookup.ResolveCsNameAsync(firstRow.CustomerServiceId);
                var order = Order.Create(firstRow.ReportNumber, firstRow.OrderEntryPerson, csName, dto.Remark);

                foreach (var row in dto.Rows)
                {
                    order.AddLine(
                        testGroup: row.TestGroup!,
                        express: await _workdayCalculator.ComputeExpressAsync(
                            row.LabIn ?? DateTimeOffset.UtcNow,
                            row.DueDate ?? DateTimeOffset.UtcNow),
                        dueDate: row.DueDate ?? DateTimeOffset.UtcNow,
                        labIn: row.LabIn ?? DateTimeOffset.UtcNow,
                        remark: row.Remark,
                        rfidCode: row.RfidCode);
                }

                await _repository.AddAsync(order, CancellationToken.None);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AddOrder failed");
                return false;
            }
        }

        /// <summary>
        /// 更新订单行 — 成功返回 true，失败返回 false
        /// </summary>
        public async Task<bool> UpdateOrderAsync(UpdateOrderRequest dto)
        {
            if (dto?.Rows == null || dto.Rows.Count == 0) return false;

            try
            {
                // 阶段 1: 收集 (lineId, orderId, row) 映射
                var pending = new List<(Guid lineId, string orderId, UpdateOrderItem row)>();
                foreach (var row in dto.Rows)
                {
                    if (row.LineId == null || !Guid.TryParse(row.LineId, out var lineId))
                        continue;

                    var orderId = await _repository.GetOrderIdByLineIdAsync(lineId, CancellationToken.None);
                    if (orderId == null) continue;

                    pending.Add((lineId, orderId, row));
                }

                // 阶段 2: 按 orderId 分组，每组加载一次 → 修改所有行 → 保存一次
                foreach (var group in pending.GroupBy(x => x.orderId))
                {
                    var order = await _repository.GetByIdAsync(new OrderId(group.Key), CancellationToken.None);
                    if (order == null) continue;

                    foreach (var (lineId, _, row) in group)
                    {
                        order.UpdateLine(lineId, new UpdateLineCommand(
                            Express: row.Express.ToOrderExpress(),
                            DueDate: row.DueDate,
                            LabIn: row.LabIn,
                            SampleCount: row.SampleCount,
                            ItemCount: row.ItemCount,
                            Reviewer: await ResolveUserName(row.ReviewerId),
                            Remark: row.Remark,
                            DelayType: row.DelayType,
                            DelayReason: row.DelayReason
                        ));

                        order.ApplyTimeBasedStatusTransition(lineId,
                            reviewer: await ResolveUserName(row.ReviewerId),
                            reviewFinishTime: row.ReviewFinishTime,
                            labOutTime: row.LabOutTime);
                    }

                    await _repository.UpdateAsync(order, CancellationToken.None);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateOrder failed");
                return false;
            }
        }

        /// <summary>
        /// 软删除订单行 — 遍历 OrderDeleteRequest.Items，逐行删除
        /// </summary>
        public async Task<bool> DeleteOrderAsync(DeleteOrderRequest req)
        {
            if (req?.Items == null || req.Items.Count == 0) return false;

            try
            {
                foreach (var item in req.Items)
                {
                    if (!Guid.TryParse(item.LineId, out var recordId)) continue;

                    var orderId = await _repository.GetOrderIdByLineIdAsync(recordId, CancellationToken.None);
                    if (orderId == null) continue;

                    var order = await _repository.GetByIdAsync(new OrderId(orderId), CancellationToken.None);
                    if (order == null) continue;

                    order.DeleteLine(recordId);
                    await _repository.UpdateAsync(order, CancellationToken.None);
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteOrder failed");
                return false;
            }
        }

        public async Task<Order?> GetOrderByIdAsync(string reportNumber)
        {
            return await _repository.GetByIdAsync(new OrderId(reportNumber), CancellationToken.None);
        }

        private async Task<string?> ResolveUserName(string? userId)
        {
            return await _lookup.ResolveUserNameAsync(userId);
        }
    }
}
