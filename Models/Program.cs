using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NX_lims_Softlines_Command_System.Models;

public partial class Program
{
    [Key]
    public long Id { get; set; }

    public string ProgramName { get; set; } = null!;

    public string? ProgramType { get; set; }

    public string? StandardType { get; set; }
}
