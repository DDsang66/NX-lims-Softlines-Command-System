using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NX_lims_Softlines_Command_System.Models;

public partial class SpecialCareInstruction
{
    [Key]
    public long Id { get; set; }

    public string Sci { get; set; } = null!;
}
