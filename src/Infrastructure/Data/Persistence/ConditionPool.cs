using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class ConditionPool
{
    public Guid ConditionPoolId { get; set; }

    public string? Conditions { get; set; }

    public DateTime CreatedAt { get; set; }

    public byte Status { get; set; }

    public Guid CheckListId { get; set; }
}
