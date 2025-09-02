using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NX_lims_Softlines_Command_System.Models;

public partial class DryProcedure
{
    [Key]
    public long Id { get; set; }

    public string DProcedure { get; set; } = null!;
}
