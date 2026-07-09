using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class BasicStandard
{
    public string IdStandard { get; set; } = null!;

    public string StandardCode { get; set; } = null!;

    public string? StandardCodeNameEn { get; set; }

    public string? StandardCodeNameChn { get; set; }

    public byte Status { get; set; }

    public string? StandardFamilyCodeId { get; set; }
}
