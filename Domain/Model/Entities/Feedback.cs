using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class Feedback
{
    public long Id { get; set; }

    public string? Type { get; set; }

    public string? FeedbackDetail { get; set; }

    public string? Status { get; set; }

    public DateTimeOffset? CreateTime { get; set; }

    public string? IsDone { get; set; }

    public string? Applicant { get; set; }
}
