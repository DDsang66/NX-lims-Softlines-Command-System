using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NX_lims_Softlines_Command_System.Models;

public partial class ParameterType
{
    [Key]
    public long Id { get; set; }

    public string ParameterType1 { get; set; } = null!;

    public string? Text { get; set; }
}
