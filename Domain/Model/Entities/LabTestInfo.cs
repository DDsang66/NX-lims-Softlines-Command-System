using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class LabTestInfo
{
    public long Id { get; set; }

    public string? ReportNumber { get; set; }

    public Guid? OrderId { get; set; }

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

    public DateTimeOffset? LastUpdateTime { get; set; }

    public string? IsDelete { get; set; }

    public DateTimeOffset? ReportDueDate { get; set; }

    public DateTimeOffset? OrderInTime { get; set; }

    public DateTimeOffset? ReviewFinishTime { get; set; }

    public DateTimeOffset? LabOutTime { get; set; }

    public string? DelayType { get; set; }

    public string? DelayReason { get; set; }

    public byte[] RowVersion { get; set; } = null!;
}
