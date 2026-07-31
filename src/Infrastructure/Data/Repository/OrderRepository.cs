using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.OrderContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class OrderRepository : IOrderRepository, IScopedDependency
    {
        private readonly LabDbContextSec _context;
        private readonly ILogger<OrderRepository> _logger;

        public OrderRepository(LabDbContextSec context, ILogger<OrderRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 新增订单 — 聚合根的所有行批量插入 LabTestInfos 表
        /// </summary>
        public async Task AddAsync(Order order, CancellationToken ct)
        {
            var entities = new List<LabTestInfo>();

            foreach (var line in order.Lines)
            {
                entities.Add(MapToEntity(order, line));
            }

            _context.LabTestInfos.AddRange(entities);
            await _context.SaveChangesAsync(ct);
        }

        /// <summary>
        /// 更新订单 — 按 line.Id 逐行覆盖 LabTestInfos 表
        /// </summary>
        public async Task UpdateAsync(Order order, CancellationToken ct)
        {
            var lineIds = order.Lines.Select(l => l.Id).ToList();
            var existingRows = await _context.LabTestInfos
                .Where(i => i.ReportNumber == order.Id.Value && lineIds.Contains(i.Id))
                .ToListAsync(ct);

            foreach (var line in order.Lines)
            {
                var row = existingRows.FirstOrDefault(r => r.Id == line.Id);
                if (row == null) continue;

                row.TestGroup = line.TestGroup;
                row.Status = (byte)line.Status;
                row.Express = line.Express.ToDisplayString();
                row.ReportDueDate = line.DueDate;
                row.OrderInTime = line.LabIn;
                row.ReviewFinishTime = line.ReviewFinishTime;
                row.LabOutTime = line.LabOutTime;
                row.Reviewer = line.Reviewer;
                row.TestSampleNum = line.SampleCount;
                row.TestItemNum = line.ItemCount;
                row.Remark = line.Remark;
                row.RfidCode = line.RfidCode;
                row.DelayType = line.Delay.Type;
                row.DelayReason = line.Delay.Reason;
                row.IsDelete = line.IsDeleted ? "Y" : "N";
            }

            await _context.SaveChangesAsync(ct);
        }

        /// <summary>
        /// 根据 ReportNumber（即 OrderId）重建订单聚合根
        /// </summary>
        public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct)
        {
            var rows = await _context.LabTestInfos
                .Where(i => i.ReportNumber == id.Value && i.IsDelete == "N")
                .ToListAsync(ct);

            if (rows.Count == 0) return null;

            var first = rows.First();
            var metadata = OrderMetadata.Create(
                first.OrderEntryPerson ?? string.Empty,
                first.CustomerService ?? string.Empty,
                null,
                first.LastUpdateTime ?? DateTimeOffset.UtcNow);

            var lines = rows.Select(MapToLine).ToList();

            return Order.Reconstitute(id, metadata, lines);
        }

        /// <summary>
        /// 判断 ReportNumber + TestGroup 重复记录
        /// </summary>
        public async Task<bool> ExistsAsync(OrderId id, string testGroup, CancellationToken ct)
        {
            return await _context.LabTestInfos
                .AnyAsync(i => i.ReportNumber == id.Value
                    && i.TestGroup == testGroup
                    && i.IsDelete == "N", ct);
        }

        public async Task<string?> GetOrderIdByLineIdAsync(Guid lineId, CancellationToken ct)
        {
            return await _context.LabTestInfos
                .Where(i => i.Id == lineId && i.IsDelete == "N")
                .Select(i => i.ReportNumber)
                .FirstOrDefaultAsync(ct);
        }

        /* ================================================================
         * 映射工具
         * ================================================================ */

        private static LabTestInfo MapToEntity(Order order, OrderLine line)
        {
            return new LabTestInfo
            {
                Id = line.Id,
                ReportNumber = order.Id.Value,
                OrderId = order.Id.Value,
                OrderEntryPerson = order.Metadata.OrderEntryPerson,
                CustomerService = order.Metadata.CustomerService,
                TestGroup = line.TestGroup,
                Status = (byte)line.Status,
                Express = line.Express.ToDisplayString(),
                ReportDueDate = line.DueDate,
                OrderInTime = line.LabIn,
                ReviewFinishTime = line.ReviewFinishTime,
                LabOutTime = line.LabOutTime,
                Reviewer = line.Reviewer,
                TestSampleNum = line.SampleCount,
                TestItemNum = line.ItemCount,
                Remark = line.Remark,
                RfidCode = line.RfidCode,
                DelayType = line.Delay.Type,
                DelayReason = line.Delay.Reason,
                IsDelete = line.IsDeleted ? "Y" : "N",
                LastUpdateTime = order.Metadata.LastUpdateTime
            };
        }

        private OrderLine MapToLine(LabTestInfo row)
        {
            if (row.Status == null)
                _logger.LogWarning("OrderLine {Id}: Status is NULL in DB, defaulting to EntryComplete", row.Id);
            if (row.ReportDueDate == null)
                _logger.LogWarning("OrderLine {Id}: ReportDueDate is NULL in DB, defaulting to UtcNow", row.Id);
            if (row.OrderInTime == null)
                _logger.LogWarning("OrderLine {Id}: OrderInTime is NULL in DB, defaulting to UtcNow", row.Id);
            if (row.TestSampleNum == null)
                _logger.LogWarning("OrderLine {Id}: TestSampleNum is NULL in DB, defaulting to 0", row.Id);
            if (row.TestItemNum == null)
                _logger.LogWarning("OrderLine {Id}: TestItemNum is NULL in DB, defaulting to 0", row.Id);

            var line = new OrderLine
            {
                TestGroup = row.TestGroup ?? string.Empty,
                Status = (OrderLineStatus)(row.Status ?? 1),
                Express = row.Express.ToOrderExpress(),
                Reviewer = row.Reviewer,
                DueDate = row.ReportDueDate ?? DateTimeOffset.UtcNow,
                LabIn = row.OrderInTime ?? DateTimeOffset.UtcNow,
                ReviewFinishTime = row.ReviewFinishTime,
                LabOutTime = row.LabOutTime,
                SampleCount = row.TestSampleNum ?? 0,
                ItemCount = row.TestItemNum ?? 0,
                Remark = row.Remark,
                RfidCode = row.RfidCode,
                Delay = DelayInfo.Create(row.DelayType, row.DelayReason),
                IsDeleted = row.IsDelete == "Y"
            };
            line.ReconstructId(row.Id);
            return line;
        }
    }
}
