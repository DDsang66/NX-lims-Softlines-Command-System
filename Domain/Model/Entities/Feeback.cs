using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class Feeback
{
    public long Id { get; set; }

    public byte Type { get; set; }

    public string? Message { get; set; }

    public byte Status { get; set; }

    public DateTime? CreateTime { get; set; }

    public string? Assignee { get; set; }
}
