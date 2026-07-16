using Microsoft.EntityFrameworkCore;
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

        public OrderRepository(LabDbContextSec context)
        {
            _context = context;
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
                .Where(i => i.ReportNumber == order.ReportNumber && lineIds.Contains(i.Id))
                .ToListAsync(ct);

            foreach (var line in order.Lines)
            {
                var row = existingRows.FirstOrDefault(r => r.Id == line.Id);
                if (row == null) continue;

                row.TestGroup = line.TestGroup;
                row.Status = (byte)line.Status;
                row.Express = ExpressToString(line.Express);
                row.ReportDueDate = line.DueDate;
                row.OrderInTime = line.LabIn;
                row.ReviewFinishTime = line.ReviewFinishTime;
                row.LabOutTime = line.LabOutTime;
                row.Reviewer = line.Reviewer;
                row.TestEngineer = line.TestEngineer;
                row.TestSampleNum = line.SampleCount;
                row.TestItemNum = line.ItemCount;
                row.Remark = line.Remark;
                row.DelayType = line.Delay.Type;
                row.DelayReason = line.Delay.Reason;
                row.IsDelete = line.IsDeleted ? "Y" : "N";
            }

            await _context.SaveChangesAsync(ct);
        }

        /// <summary>
        /// 根据 ReportNumber 重建订单聚合根
        /// </summary>
        public async Task<Order?> GetByIdAsync(OrderId id, CancellationToken ct)
        {
            var rows = await _context.LabTestInfos
                .Where(i => i.OrderId == id.Value && i.IsDelete == "N")
                .ToListAsync(ct);

            if (rows.Count == 0) return null;

            var first = rows.First();
            var metadata = OrderMetadata.Create(
                first.OrderEntryPerson ?? string.Empty,
                first.CustomerService ?? string.Empty,
                null,  // remark 在行级别
                first.LastUpdateTime ?? DateTimeOffset.UtcNow);

            var lines = rows.Select(MapToLine).ToList();

            return Order.Reconstitute(id, first.ReportNumber ?? string.Empty, metadata, lines);
        }

        /// <summary>
        /// 判断 ReportNumber + TestGroup 重复记录
        /// </summary>
        public async Task<bool> ExistsAsync(OrderId id, string testGroup, CancellationToken ct)
        {
            return await _context.LabTestInfos
                .AnyAsync(i => i.OrderId == id.Value
                    && i.TestGroup == testGroup
                    && i.IsDelete == "N", ct);
        }

        public async Task<Guid?> GetOrderIdByLineIdAsync(long lineId, CancellationToken ct)
        {
            return await _context.LabTestInfos
                .Where(i => i.Id == lineId && i.IsDelete == "N")
                .Select(i => i.OrderId)
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
                OrderId = order.Id.Value,
                ReportNumber = order.ReportNumber,
                OrderEntryPerson = order.Metadata.OrderEntryPerson,
                CustomerService = order.Metadata.CustomerService,
                TestGroup = line.TestGroup,
                Status = (byte)line.Status,
                Express = ExpressToString(line.Express),
                ReportDueDate = line.DueDate,
                OrderInTime = line.LabIn,
                ReviewFinishTime = line.ReviewFinishTime,
                LabOutTime = line.LabOutTime,
                Reviewer = line.Reviewer,
                TestEngineer = line.TestEngineer,
                TestSampleNum = line.SampleCount,
                TestItemNum = line.ItemCount,
                Remark = line.Remark,
                DelayType = line.Delay.Type,
                DelayReason = line.Delay.Reason,
                IsDelete = line.IsDeleted ? "Y" : "N",
                LastUpdateTime = order.Metadata.LastUpdateTime
            };
        }

        private static OrderLine MapToLine(LabTestInfo row)
        {
            return new OrderLine
            {
                Id = row.Id,
                TestGroup = row.TestGroup ?? string.Empty,
                Status = (OrderLineStatus)(row.Status ?? 1),
                Express = StringToExpress(row.Express),
                Reviewer = row.Reviewer,
                TestEngineer = row.TestEngineer,
                DueDate = row.ReportDueDate ?? DateTimeOffset.UtcNow,
                LabIn = row.OrderInTime ?? DateTimeOffset.UtcNow,
                ReviewFinishTime = row.ReviewFinishTime,
                LabOutTime = row.LabOutTime,
                SampleCount = row.TestSampleNum ?? 0,
                ItemCount = row.TestItemNum ?? 0,
                Remark = row.Remark,
                Delay = DelayInfo.Create(row.DelayType, row.DelayReason),
                IsDeleted = row.IsDelete == "Y"
            };
        }

        private static string ExpressToString(OrderExpress express) => express switch
        {
            OrderExpress.SameDay => "Same Day",
            OrderExpress.Shuttle => "Shuttle",
            OrderExpress.Express => "Express",
            _ => "Regular"
        };

        private static OrderExpress StringToExpress(string? s) => s switch
        {
            "Same Day" => OrderExpress.SameDay,
            "Shuttle" => OrderExpress.Shuttle,
            "Express" => OrderExpress.Express,
            _ => OrderExpress.Regular
        };
    }
}
