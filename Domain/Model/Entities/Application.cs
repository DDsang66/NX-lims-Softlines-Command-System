using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class Application
{
    public long ApplicationId { get; set; }

    public string? Applicant { get; set; }

    public string? Reason { get; set; }

    public string? ReportNumber { get; set; }

    public string? TestGroup { get; set; }

    public DateTimeOffset? ReportDueDate { get; set; }

    public string? Express { get; set; }

    public int? TestSampleNum { get; set; }

    public string? Remark { get; set; }
}
