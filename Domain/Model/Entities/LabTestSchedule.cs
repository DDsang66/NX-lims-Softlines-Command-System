using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class LabTestSchedule
{
    public long IdSchedule { get; set; }

    public DateOnly? ReportDueDate { get; set; }

    public DateTime? OrderInTime { get; set; }

    public DateTime? ReviewFinishTime { get; set; }

    public DateTime? LabOutTime { get; set; }
}
