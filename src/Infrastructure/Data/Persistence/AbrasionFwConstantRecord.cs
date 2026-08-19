using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class AbrasionFwConstantRecord
{
    public int ConstantRecordId { get; set; }

    public string Type { get; set; } = null!;

    public double Value { get; set; }

    public string Modifier { get; set; } = null!;

    public string? Reason { get; set; }

    public DateTime ModifiedAt { get; set; }
}
