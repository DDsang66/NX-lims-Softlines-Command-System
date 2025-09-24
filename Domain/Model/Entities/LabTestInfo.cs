using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class LabTestInfo
{
    public long Id { get; set; }

    public string? ReportNumber { get; set; }

    public string? Reviewer { get; set; }

    public string? TestEngineer { get; set; }

    public string? OrderEntryPerson { get; set; }

    public string? CustomerService { get; set; }

    public byte? Status { get; set; }

    public string? TestGroup { get; set; }

    public int? TestSampleNum { get; set; }

    public int? TestItemNum { get; set; }

    public string? Remark { get; set; }

    public string? Express { get; set; }

    public string? Describe { get; set; }

    public long ScheduleIndex { get; set; }
    public DateTime LastUpdateTime { get; set; }
}
