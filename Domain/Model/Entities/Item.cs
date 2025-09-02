using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class Item
{
    [Key]
    public int ItemIndex { get; set; }

    public string? ItemName { get; set; }

    public string? Type { get; set; }
}
