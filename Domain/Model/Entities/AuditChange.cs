using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class AuditChange
{
    public long ChangeRecordId { get; set; }

    public string? ReportNumber { get; set; }

    public string? TestGroup { get; set; }

    public string? ColumnName { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? ChangePerson { get; set; }

    public DateTimeOffset? ChangeTime { get; set; }

    public string? Remark { get; set; }
}
