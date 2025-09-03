using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class Item
{
    public int ItemIndex { get; set; }

    public string? ItemName { get; set; }

    public string? Type { get; set; }
}
