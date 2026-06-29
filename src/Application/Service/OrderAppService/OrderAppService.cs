using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.AuthenticationService;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.OrderContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.OrderAppService
{
    public class OrderAppService : IScopedDependency
    {
        private readonly IOrderRepository _repository;
        private readonly LabDbContextSec _db;

        public OrderAppService(IOrderRepository repository, LabDbContextSec db)
        {
            _repository = repository;
            _db = db;
        }

        /// <summary>
        /// 新增订单
        /// </summary>
        public async Task AddOrderAsync(OrderDto dto)
        {
            if (dto == null || dto.Rows == null || dto.Rows.Count == 0)
                throw new ArgumentException("订单数据不能为空");

            var firstRow = dto.Rows.First();
            if (firstRow.ReportNum == null || firstRow.OrderEntry == null)
                throw new ArgumentException("报告号和录入人不能为空");

            // 查重复 — 聚合根内会再检查，这里提前给友好错误
            foreach (var row in dto.Rows)
            {
                if (await _repository.ExistsAsync(new OrderId(row.ReportNum!), row.Group!, CancellationToken.None))
                    throw new InvalidOperationException($"重复记录: ReportNum={row.ReportNum}, Group={row.Group}");
            }

            // 创建聚合根
            var csName = _db.CustomerServices.FirstOrDefault(i => i.Id == dto.Rows[0].Cs)?.CustomerService1 ?? string.Empty;
            var order = Order.Create(firstRow.ReportNum, firstRow.OrderEntry, csName, dto.Remark);

            // 添加行
            foreach (var row in dto.Rows)
            {
                order.AddLine(
                    lineId: new SnowflakeIdGenerator().NextId(),
                    testGroup: row.Group!,
                    express: StringToExpress(row.Express),
                    dueDate: row.DueDate ?? DateTimeOffset.UtcNow,
                    labIn: row.LabIn ?? DateTimeOffset.UtcNow,
                    remark: row.Remark);
            }

            await _repository.AddAsync(order, CancellationToken.None);
        }

        /// <summary>
        /// 更新订单行
        /// </summary>
        public async Task UpdateOrderAsync(OrderUpdateDto dto)
        {
            if (dto == null || dto.Rows == null || dto.Rows.Count == 0)
                throw new ArgumentException("更新数据不能为空");

            foreach (var row in dto.Rows)
            {
                if (row.RecordId == null || !long.TryParse(row.RecordId, out var lineId))
                    throw new ArgumentException("RecordId 无效");

                // 从 RecordId 找到对应的 ReportNumber（DbContext 查询）
                var record = await _db.LabTestInfos
                    .FirstOrDefaultAsync(i => i.Id == lineId && i.IsDelete == "N");
                if (record?.ReportNumber == null)
                    throw new InvalidOperationException($"记录 {lineId} 不存在或已删除");

                var order = await _repository.GetByIdAsync(new OrderId(record.ReportNumber), CancellationToken.None);
                if (order == null)
                    throw new InvalidOperationException($"订单 {record.ReportNumber} 不存在");

                // 更新行数据
                order.UpdateLine(
                    lineId,
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

                // 时间驱动的状态自动转换
                order.ApplyTimeBasedStatusTransition(
                    lineId,
                    reviewer: await ResolveUserName(row.ReviewerId),
                    engineer: row.TestEngineer,
                    reviewFinishTime: row.ReviewFinishTime,
                    labOutTime: row.LabOutTime);

                await _repository.UpdateAsync(order, CancellationToken.None);
            }
        }

        /// <summary>
        /// 软删除一行
        /// </summary>
        public async Task DeleteOrderAsync(long recordId)
        {
            var record = await _db.LabTestInfos
                .FirstOrDefaultAsync(i => i.Id == recordId && i.IsDelete == "N");
            if (record?.ReportNumber == null)
                throw new InvalidOperationException($"记录 {recordId} 不存在或已删除");

            var order = await _repository.GetByIdAsync(new OrderId(record.ReportNumber), CancellationToken.None);
            if (order == null)
                throw new InvalidOperationException($"订单 {record.ReportNumber} 不存在");

            order.DeleteLine(recordId);
            await _repository.UpdateAsync(order, CancellationToken.None);
        }

        /// <summary>
        /// 根据 ReportNumber 加载订单
        /// </summary>
        public Task<Order?> GetOrderByIdAsync(string reportNumber)
        {
            return _repository.GetByIdAsync(new OrderId(reportNumber), CancellationToken.None);
        }

        /* ================================================================
         * 辅助
         * ================================================================ */

        private static OrderExpress StringToExpress(string? s) => s switch
        {
            "Same Day" => OrderExpress.SameDay,
            "Shuttle" => OrderExpress.Shuttle,
            "Express" => OrderExpress.Express,
            _ => OrderExpress.Regular
        };

        private async Task<string?> ResolveUserName(string? userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return null;
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            return user?.NickName;
        }
    }
}
