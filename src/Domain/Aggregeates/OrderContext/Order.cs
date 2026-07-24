using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Events;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext
{
    /// <summary>
    /// 订单聚合根
    /// 一个订单 = 同一 ReportNumber 下的多个 OrderLine（按 TestGroup 拆分）
    /// </summary>
    public sealed class Order : AggregateRoot<OrderId, string>
    {
        private readonly List<OrderLine> _lines = new();
        /// <summary>
        /// 订单行（只读）
        /// </summary>
        public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

        /// <summary>
        /// 订单元数据
        /// </summary>
        public OrderMetadata Metadata { get; private set; } = null!;

        /// <summary>
        /// 余样码（订单级别，进单时分配）
        /// </summary>
        public string? ResidualSampleCode { get; private set; }

        /* ================================================================
         * 工厂方法
         * ================================================================ */

        /// <summary>
        /// 创建新订单 — ReportNumber 即是订单标识
        /// </summary>
        /// <param name="reportNumber">报告号</param>
        /// <param name="orderEntryPerson">录入人</param>
        /// <param name="customerService">客服</param>
        /// <param name="remark">备注</param>
        /// <returns>新创建的订单聚合根</returns>
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

            order.AddDomainEvent(new OrderCreatedEvent(order.Id));

            return order;
        }

        /// <summary>
        /// 从持久化重建订单（由 Repository 调用）
        /// </summary>
        public static Order Reconstitute(OrderId id, OrderMetadata metadata, IEnumerable<OrderLine> lines, string? residualSampleCode = null)
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));

            var order = new Order
            {
                Id = id,
                Metadata = metadata,
                ResidualSampleCode = residualSampleCode
            };
            // 逐行校验：跳过已删除行，拒绝重复 TestGroup
            if (lines != null)
            {
                foreach (var line in lines)
                {
                    if (line.IsDeleted) continue;
                    if (order._lines.Any(l => l.TestGroup == line.TestGroup && !l.IsDeleted))
                        throw new InvalidOperationException(
                            $"Duplicate TestGroup '{line.TestGroup}' in reconstituted order");
                    order._lines.Add(line);
                }
            }
            return order;
        }

        /* ================================================================
         * 行管理
         * ================================================================ */

        /// <summary>
        /// 添加订单行 — TestGroup 不可重复，DueDate/LabIn 必填
        /// 行的 Guid Id 由 Entity 基类自动生成
        /// </summary>
        public void AddLine(
            string testGroup,
            OrderExpress express,
            DateTimeOffset dueDate,
            DateTimeOffset labIn,
            string? remark = null,
            string? rfidCode = null)
        {
            if (string.IsNullOrWhiteSpace(testGroup))
                throw new ArgumentException("Test group is required", nameof(testGroup));

            if (_lines.Any(l => l.TestGroup == testGroup && !l.IsDeleted))
                throw new InvalidOperationException($"Test group '{testGroup}' already exists in this order");

            var line = new OrderLine
            {
                TestGroup = testGroup,
                Status = OrderLineStatus.EntryComplete,
                Express = express,
                DueDate = dueDate,
                LabIn = labIn,
                RfidCode = rfidCode,
                Remark = remark
            };

            _lines.Add(line);

            AddDomainEvent(new OrderLineAddedEvent(Id, line.Id));
        }

        /* ================================================================
         * 领域行为
         * ================================================================ */

        /// <summary>
        /// 审单完成 — 由扫码站决定，不校验前置状态
        /// </summary>
        public void MarkReviewComplete(Guid lineId, string reviewer, DateTimeOffset finishTime)
        {
            var line = GetLine(lineId);
            line.MarkReviewComplete(reviewer, finishTime);
            Metadata = Metadata with { LastUpdateTime = DateTimeOffset.UtcNow };

            AddDomainEvent(new ReviewCompletedEvent(Id, lineId));
        }

        /// <summary>
        /// 进入实验室 — 由扫码站决定，不校验前置状态
        /// </summary>
        public void MarkLabIn(Guid lineId, DateTimeOffset labInTime)
        {
            var line = GetLine(lineId);
            line.MarkLabIn(labInTime);
            Metadata = Metadata with { LastUpdateTime = DateTimeOffset.UtcNow };

            AddDomainEvent(new LabInCompletedEvent(Id, lineId));
        }

        /// <summary>
        /// 测试完成 — 由扫码站决定，不校验前置状态
        /// </summary>
        public void MarkTestDone(Guid lineId, DateTimeOffset finishTime)
        {
            var line = GetLine(lineId);
            line.MarkTestDone(finishTime);
            Metadata = Metadata with { LastUpdateTime = DateTimeOffset.UtcNow };

            AddDomainEvent(new TestDoneEvent(Id, lineId));
        }

        /// <summary>
        /// 报告已出 — 由扫码站决定，不校验前置状态
        /// </summary>
        public void MarkReportOut(Guid lineId, DateTimeOffset reportTime)
        {
            var line = GetLine(lineId);
            line.MarkReportOut(reportTime);
            Metadata = Metadata with { LastUpdateTime = DateTimeOffset.UtcNow };

            AddDomainEvent(new ReportOutEvent(Id, lineId));
        }

        /// <summary>
        /// 更新行数据
        /// </summary>
        public void UpdateLine(Guid lineId, UpdateLineCommand cmd)
        {
            var line = GetLine(lineId);
            line.Update(cmd.Express, cmd.DueDate, cmd.LabIn, cmd.SampleCount, cmd.ItemCount,
                cmd.Reviewer, cmd.Remark, cmd.DelayType, cmd.DelayReason);
            Metadata = Metadata with { LastUpdateTime = DateTimeOffset.UtcNow };

            AddDomainEvent(new OrderLineUpdatedEvent(Id, lineId));
        }

        /// <summary>
        /// 根据时间字段自动推进状态：
        /// ReviewFinishTime → Entry→ReviewComplete | LabIn → ReviewComplete→InLab |
        /// ReviewFinishTime → InLab→TestDone | LabOutTime → TestDone→ReportOut
        /// </summary>
        public void ApplyTimeBasedStatusTransition(
            Guid lineId,
            string? reviewer,
            DateTimeOffset? reviewFinishTime,
            DateTimeOffset? labOutTime)
        {
            var line = GetLine(lineId);

            // ReviewFinishTime 有值且状态为 EntryComplete → MarkReviewComplete
            if (reviewFinishTime.HasValue && line.Status == OrderLineStatus.EntryComplete)
            {
                MarkReviewComplete(lineId, reviewer ?? string.Empty, reviewFinishTime.Value);
            }

            // LabIn 有值且状态为 ReviewComplete → MarkLabIn
            if (line.LabIn != default && line.Status == OrderLineStatus.ReviewComplete)
            {
                MarkLabIn(lineId, line.LabIn);
            }

            // ReviewFinishTime 有值且状态为 InLab → MarkTestDone
            if (reviewFinishTime.HasValue && line.Status == OrderLineStatus.InLab)
            {
                MarkTestDone(lineId, reviewFinishTime.Value);
            }

            // LabOutTime 有值且状态为 TestDone → MarkReportOut
            if (labOutTime.HasValue && line.Status == OrderLineStatus.TestDone)
            {
                MarkReportOut(lineId, labOutTime.Value);
            }
        }

        /// <summary>
        /// 软删除一行
        /// </summary>
        public void DeleteLine(Guid lineId)
        {
            var line = GetLine(lineId);
            line.Delete();
            Metadata = Metadata with { LastUpdateTime = DateTimeOffset.UtcNow };

            AddDomainEvent(new OrderLineDeletedEvent(Id, lineId));
        }

        /* ================================================================
         * 查询
         * ================================================================ */

        /// <summary>
        /// 判断是否已有同 TestGroup 的未删除行
        /// </summary>
        public bool HasDuplicateGroup(string testGroup)
            => _lines.Any(l => l.TestGroup == testGroup && !l.IsDeleted);

        private OrderLine GetLine(Guid lineId)
        {
            var line = _lines.FirstOrDefault(l => l.Id == lineId && !l.IsDeleted);
            if (line == null)
                throw new InvalidOperationException($"Line {lineId} not found or deleted");
            return line;
        }
    }
}
