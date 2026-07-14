using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext
{
    /// <summary>
    /// 订单聚合根
    /// —个订单 = 同一 ReportNumber 下的多个 OrderLine（按 TestGroup 拆分）
    /// </summary>
    public sealed class Order : AggregateRoot
    {
        private readonly List<OrderLine> _lines = new();

        public OrderId Id { get; private set; } = null!;

        /// <summary>
        /// 订单行（只读）
        /// </summary>
        public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

        /// <summary>
        /// 订单元数据
        /// </summary>
        public OrderMetadata Metadata { get; private set; } = null!;

        private Order() { }

        /* ================================================================
         * 工厂方法
         * ================================================================ */

        /// <summary>
        /// 创建新订单 — 同时创建多行（每个 TestGroup 一行）
        /// </summary>
        /// <param name="reportNumber">报告号</param>
        /// <param name="orderEntryPerson">录入人</param>
        /// <param name="customerService">客服</param>
        /// <param name="remark">备注</param>
        /// <returns>新创建的订单聚合根</returns>
        /// <exception cref="ArgumentException">报告号为空或重复 TestGroup</exception>
        public static Order Create(
            string reportNumber,
            string orderEntryPerson,
            string customerService,
            string? remark)
        {
            if (string.IsNullOrWhiteSpace(reportNumber))
                throw new ArgumentException("Report number is required", nameof(reportNumber));

            if (string.IsNullOrWhiteSpace(orderEntryPerson))
                throw new ArgumentException("Order entry person is required", nameof(orderEntryPerson));

            var order = new Order
            {
                Id = new OrderId(reportNumber),
                Metadata = OrderMetadata.Create(orderEntryPerson, customerService, remark, DateTimeOffset.UtcNow)
            };

            //order.AddDomainEvent(new OrderCreatedEvent(order.Id));

            return order;
        }

        /// <summary>
        /// 从持久化重建订单（由 Repository 调用）
        /// </summary>
        public static Order Reconstitute(OrderId id, OrderMetadata metadata, IEnumerable<OrderLine> lines)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));

            var order = new Order { Id = id, Metadata = metadata };
            if (lines != null) order._lines.AddRange(lines);
            return order;
        }

        /* ================================================================
         * 行管理
         * ================================================================ */

        /// <summary>
        /// 添加订单行 — TestGroup 不可重复，DueDate/LabIn 必填
        /// </summary>
        public void AddLine(
            long lineId,
            string testGroup,
            OrderExpress? express,
            DateTimeOffset dueDate,
            DateTimeOffset labIn,
            string? remark = null)
        {
            if (string.IsNullOrWhiteSpace(testGroup))
                throw new ArgumentException("Test group is required", nameof(testGroup));

            if (_lines.Any(l => l.TestGroup == testGroup && !l.IsDeleted))
                throw new InvalidOperationException($"Test group '{testGroup}' already exists in this order");

            var expr = express ?? OrderLine.ComputeExpress(dueDate, labIn);

            var line = new OrderLine
            {
                Id = lineId,
                TestGroup = testGroup,
                Status = OrderLineStatus.InLab,
                Express = expr,
                DueDate = dueDate,
                LabIn = labIn,
                Remark = remark
            };

            _lines.Add(line);

            //AddDomainEvent(new OrderLineAddedEvent(Id, lineId));
        }

        /* ================================================================
         * 领域行为
         * ================================================================ */

        /// <summary>
        /// 标记一条行为"审核完成"
        /// </summary>
        public void MarkReviewFinished(long lineId, string reviewer, DateTimeOffset finishTime)
        {
            var line = GetLine(lineId);
            line.MarkReviewFinished(reviewer, finishTime);
            Metadata = Metadata with { LastUpdateTime = DateTimeOffset.UtcNow };

            //AddDomainEvent(new ReviewCompletedEvent(Id, lineId));
        }

        /// <summary>
        /// 标记一条行为"已出实验室"
        /// </summary>
        public void MarkLabOut(long lineId, string engineer, DateTimeOffset labOutTime)
        {
            var line = GetLine(lineId);
            line.MarkLabOut(engineer, labOutTime);
            Metadata = Metadata with { LastUpdateTime = DateTimeOffset.UtcNow };

            //AddDomainEvent(new LabOutCompletedEvent(Id, lineId));
        }

        /// <summary>
        /// 更新行数据
        /// </summary>
        public void UpdateLine(
            long lineId,
            OrderExpress? express = null,
            DateTimeOffset? dueDate = null,
            DateTimeOffset? labIn = null,
            int? sampleCount = null,
            int? itemCount = null,
            string? reviewer = null,
            string? engineer = null,
            string? remark = null,
            string? delayType = null,
            string? delayReason = null)
        {
            var line = GetLine(lineId);
            line.Update(express, dueDate, labIn, sampleCount, itemCount, reviewer, engineer,
                remark, delayType, delayReason);
            Metadata = Metadata with { LastUpdateTime = DateTimeOffset.UtcNow };

            //AddDomainEvent(new OrderLineUpdatedEvent(Id, lineId));
        }

        /// <summary>
        /// 处理审核相关的状态变更：若设置 ReviewFinishTime → 自动 ReviewFinished；若设置 LabOutTime → 自动 TestDone
        /// </summary>
        public void ApplyTimeBasedStatusTransition(
            long lineId,
            string? reviewer,
            string? engineer,                    // 出实验室时的工程师
            DateTimeOffset? reviewFinishTime,
            DateTimeOffset? labOutTime)
        {
            var line = GetLine(lineId);

            if (reviewFinishTime.HasValue && line.Status == OrderLineStatus.InLab)
            {
                line.MarkReviewFinished(reviewer ?? string.Empty, reviewFinishTime.Value);
            }

            if (labOutTime.HasValue && line.Status == OrderLineStatus.ReviewFinished)
            {
                line.MarkLabOut(engineer ?? string.Empty, labOutTime.Value);
            }

            Metadata = Metadata with { LastUpdateTime = DateTimeOffset.UtcNow };
        }

        /// <summary>
        /// 软删除一行
        /// </summary>
        public void DeleteLine(long lineId)
        {
            var line = GetLine(lineId);
            line.Delete();
            Metadata = Metadata with { LastUpdateTime = DateTimeOffset.UtcNow };

            //AddDomainEvent(new OrderLineDeletedEvent(Id, lineId));
        }

        /* ================================================================
         * 查询
         * ================================================================ */

        /// <summary>
        /// 判断是否已有同 TestGroup 的未删除行
        /// </summary>
        public bool HasDuplicateGroup(string testGroup)
            => _lines.Any(l => l.TestGroup == testGroup && !l.IsDeleted);

        private OrderLine GetLine(long lineId)
        {
            var line = _lines.FirstOrDefault(l => l.Id == lineId && !l.IsDeleted);
            if (line == null)
                throw new InvalidOperationException($"Line {lineId} not found or deleted");
            return line;
        }
    }
}
