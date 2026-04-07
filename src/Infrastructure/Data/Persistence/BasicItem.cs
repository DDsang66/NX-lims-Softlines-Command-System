using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class BasicItem
{
    public string IdItem { get; set; } = null!;

    public string ItemNameEn { get; set; } = null!;

    public string ItemNameChn { get; set; } = null!;

    public string? ItemTypeFir { get; set; }

    public string? ItemTypeSec { get; set; }
}
