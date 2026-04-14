using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class BasicBuyerMenu
{
    public string IdMenu { get; set; } = null!;

    public string MenuName { get; set; } = null!;

    public string ModifiedName { get; set; } = null!;

    public string IndexItem { get; set; } = null!;

    public string IndexStandardCode { get; set; } = null!;

    public string? Requirement { get; set; }

    public string? TestGroup { get; set; }

    public string? DisplayGroup { get; set; }

    public string BuyerCode { get; set; } = null!;
}
