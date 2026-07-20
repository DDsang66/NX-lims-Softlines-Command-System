using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext
{
    /// <summary>
    /// 订单行实体 — 一个 ReportNumber 下按 TestGroup 拆分的一行
    /// 仅由 Order 聚合根创建和修改
    /// </summary>
    public sealed class OrderLine:  Entity
    {
        public long Id { get; internal set; }
        public string TestGroup { get; internal set; } = string.Empty;
        public OrderLineStatus Status { get; internal set; } = OrderLineStatus.InLab;
        public OrderExpress Express { get; internal set; }

        // 人员
        public string? Reviewer { get; internal set; }
        public string? TestEngineer { get; internal set; }

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
        /// 计算急单类型（DueDate 与 LabIn 的天数差）
        /// </summary>
        public static OrderExpress ComputeExpress(DateTimeOffset dueDate, DateTimeOffset labIn)
        {
            var days = (dueDate - labIn).TotalDays;
            return days switch
            {
                <= 1 => OrderExpress.SameDay,
                <= 2 => OrderExpress.Shuttle,
                <= 3 => OrderExpress.Express,
                _ => OrderExpress.Regular
            };
        }

        /// <summary>
        /// 完成审核 — 状态只能从 InLab 流转到 ReviewFinished
        /// </summary>
        internal void MarkReviewFinished(string reviewer, DateTimeOffset finishTime)
        {
            if (Status != OrderLineStatus.InLab)
                throw new InvalidOperationException($"Cannot mark review: current status is {Status}");

            Reviewer = reviewer;
            ReviewFinishTime = finishTime;
            Status = OrderLineStatus.ReviewFinished;
        }

        /// <summary>
        /// 出实验室 — 状态只能从 ReviewFinished 流转到 TestDone
        /// </summary>
        internal void MarkLabOut(string engineer, DateTimeOffset labOutTime)
        {
            if (Status != OrderLineStatus.ReviewFinished)
                throw new InvalidOperationException($"Cannot mark lab-out: current status is {Status}");

            TestEngineer = engineer;
            LabOutTime = labOutTime;
            Status = OrderLineStatus.TestDone;
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
            string? engineer,
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
            if (engineer != null) TestEngineer = engineer;
            if (remark != null) Remark = remark;
            Delay = DelayInfo.Create(delayType ?? Delay.Type, delayReason ?? Delay.Reason);
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
