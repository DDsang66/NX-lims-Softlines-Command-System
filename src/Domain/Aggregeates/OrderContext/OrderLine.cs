using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext
{
    /// <summary>
    /// 订单行实体 — 一个 ReportNumber 下按 TestGroup 拆分的一行
    /// 仅由 Order 聚合根创建和修改
    /// </summary>
    public sealed class OrderLine : Entity
    {
        public long Id { get; internal set; }
        public string TestGroup { get; internal set; } = string.Empty;
        public OrderLineStatus Status { get; internal set; } = OrderLineStatus.EntryComplete;
        public OrderExpress Express { get; internal set; }

        // 人员
        public string? Reviewer { get; internal set; }

        // 时间
        public DateTimeOffset DueDate { get; internal set; }
        public DateTimeOffset LabIn { get; internal set; }
        public DateTimeOffset? ReviewFinishTime { get; internal set; }
        public DateTimeOffset? LabOutTime { get; internal set; }

        // 数量
        public int SampleCount { get; internal set; }
        public int ItemCount { get; internal set; }

        // 备注 + 延迟
        public string? Remark { get; internal set; }
        public DelayInfo Delay { get; internal set; } = DelayInfo.None();

        public bool IsDeleted { get; internal set; }

        internal OrderLine() { }  // 仅聚合根创建

        /// <summary>
        /// 审单完成 — 状态只能从 EntryComplete 流转到 ReviewComplete
        /// </summary>
        internal void MarkReviewComplete(string reviewer, DateTimeOffset finishTime)
        {
            if (Status != OrderLineStatus.EntryComplete)
                throw new InvalidOperationException($"Cannot mark review-complete: current status is {Status}");

            Reviewer = reviewer;
            ReviewFinishTime = finishTime;
            Status = OrderLineStatus.ReviewComplete;
        }

        /// <summary>
        /// 进入实验室 — 状态只能从 ReviewComplete 流转到 InLab
        /// </summary>
        internal void MarkLabIn(DateTimeOffset labInTime)
        {
            if (Status != OrderLineStatus.ReviewComplete)
                throw new InvalidOperationException($"Cannot mark lab-in: current status is {Status}");

            LabIn = labInTime;
            Status = OrderLineStatus.InLab;
        }

        /// <summary>
        /// 测试完成 — 状态只能从 InLab 流转到 TestDone
        /// </summary>
        internal void MarkTestDone(DateTimeOffset testTime)
        {
            if (Status != OrderLineStatus.InLab)
                throw new InvalidOperationException($"Cannot mark test-done: current status is {Status}");

            ReviewFinishTime = testTime;
            Status = OrderLineStatus.TestDone;
        }

        /// <summary>
        /// 报告已出 — 状态只能从 TestDone 流转到 ReportOut
        /// </summary>
        internal void MarkReportOut(DateTimeOffset reportTime)
        {
            if (Status != OrderLineStatus.TestDone)
                throw new InvalidOperationException($"Cannot mark report-out: current status is {Status}");

            LabOutTime = reportTime;
            Status = OrderLineStatus.ReportOut;
        }

        /// <summary>
        /// 更新行数据（由聚合根调用）
        /// </summary>
        internal void Update(
            OrderExpress? express,
            DateTimeOffset? dueDate,
            DateTimeOffset? labIn,
            int? sampleCount,
            int? itemCount,
            string? reviewer,
            string? remark,
            string? delayType,
            string? delayReason)
        {
            if (express.HasValue) Express = express.Value;
            if (dueDate.HasValue) DueDate = dueDate.Value;
            if (labIn.HasValue) LabIn = labIn.Value;
            if (sampleCount.HasValue) SampleCount = sampleCount.Value;
            if (itemCount.HasValue) ItemCount = itemCount.Value;
            if (reviewer != null) Reviewer = reviewer;
            if (remark != null) Remark = remark;
            Delay = DelayInfo.Create(
                string.IsNullOrWhiteSpace(delayType) ? Delay.Type : delayType,
                string.IsNullOrWhiteSpace(delayReason) ? Delay.Reason : delayReason);
        }

        /// <summary>
        /// 软删除
        /// </summary>
        internal void Delete()
        {
            if (IsDeleted) throw new InvalidOperationException("Line is already deleted");
            IsDeleted = true;
        }
    }
}
