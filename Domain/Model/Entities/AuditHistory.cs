using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class AuditHistory
{
    public long ChangeHistoryId { get; set; }

    public string? ContactTable { get; set; }

    public long? ContactId { get; set; }

    public string? ReportNumber { get; set; }

    public DateTimeOffset? LastChangeTime { get; set; }
}
