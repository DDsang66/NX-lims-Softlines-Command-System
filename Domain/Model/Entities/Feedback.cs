using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class Feedback
{
    public long Id { get; set; }

    /// <summary>
    /// 类型（建议、BUG）
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// 反馈详情
    /// </summary>
    public string? FeedbackDetail { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// 提交时间
    /// </summary>
    public DateTimeOffset? CreateTime { get; set; }

    /// <summary>
    /// 是否解决
    /// </summary>
    public string? IsDone { get; set; }

    /// <summary>
    /// 申请人
    /// </summary>
    public string? Applicant { get; set; }
}
