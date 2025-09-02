using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NX_lims_Softlines_Command_System.Models;

public partial class AdidasMtoI
{
    [Key]
    public long Id { get; set; }

    public string? Method { get; set; }

    public string? Item { get; set; }

    public string? Type { get; set; }
}
