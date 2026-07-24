using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class BasicItem
{
    public string IdItem { get; set; } = null!;

    public string ItemNameEn { get; set; } = null!;

    public string ItemNameChn { get; set; } = null!;

    public byte TestGroup { get; set; }

    public string? Description { get; set; }

    public byte Status { get; set; }

    public string? ParamRequireDenfinition { get; set; }

    public bool IsFeasible { get; set; }
}
