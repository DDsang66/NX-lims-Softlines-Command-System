using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class Standard
{
    [Key]
    public int StandardId { get; set; }

    public int? ItemIndex { get; set; }

    public string? StandardCode { get; set; }
}
