using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class LabTestSchedule
{
    public long IdSchedule { get; set; }

    public DateOnly ReportDueDate { get; set; }

    public DateTimeOffset OrderInTime { get; set; }

    public DateTimeOffset? ReviewFinishTime { get; set; }

    public DateTimeOffset? LabOutTime { get; set; }
}
