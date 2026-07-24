using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext
{
    /// <summary>
    /// 订单行实体 — 一个 ReportNumber 下按 TestGroup 拆分的一行
    /// 仅由 Order 聚合根创建和修改
    /// Id 继承自 Entity 基类的 Guid
    /// </summary>
    public sealed class OrderLine : Entity
    {
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

        // RFID + 备注 + 延迟
        public string? RfidCode { get; internal set; }
        public string? Remark { get; internal set; }
        public DelayInfo Delay { get; internal set; } = DelayInfo.None();

        public bool IsDeleted { get; internal set; }

        internal OrderLine() { }  // 仅聚合根创建

        /// <summary>
        /// 审单完成 — 由扫码站决定，不校验前置状态
        /// </summary>
        internal void MarkReviewComplete(string reviewer, DateTimeOffset finishTime)
        {
            Reviewer = reviewer;
            ReviewFinishTime = finishTime;
            Status = OrderLineStatus.ReviewComplete;
        }

        /// <summary>
        /// 进入实验室 — 由扫码站决定，不校验前置状态
        /// </summary>
        internal void MarkLabIn(DateTimeOffset labInTime)
        {
            LabIn = labInTime;
            Status = OrderLineStatus.InLab;
        }

        /// <summary>
        /// 测试完成 — 由扫码站决定，不校验前置状态
        /// </summary>
        internal void MarkTestDone(DateTimeOffset testTime)
        {
            ReviewFinishTime = testTime;
            Status = OrderLineStatus.TestDone;
        }

        /// <summary>
        /// 报告已出 — 由扫码站决定，不校验前置状态
        /// </summary>
        internal void MarkReportOut(DateTimeOffset reportTime)
        {
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
