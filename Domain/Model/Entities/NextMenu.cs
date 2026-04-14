using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class NextMenu
{
    public int IdNext { get; set; }

    public string? ItemName { get; set; }

    public string? StandardName { get; set; }

    public string? BuyerTable { get; set; }

    public string? Type { get; set; }
}
