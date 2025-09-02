using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NX_lims_Softlines_Command_System.Models;

public partial class WetParameter
{
    [Key]
    public long Id { get; set; }

    public string StandardType { get; set; } = null!;

    public string? Program { get; set; }

    public string? Temperature { get; set; }

    public int? SteelBall { get; set; }

    public string? WashingProcedure { get; set; }

    public string? DryCondition { get; set; }
    public string? DryProcedure { get; set; }

    public string? Sci { get; set; }

    public string? IsSensitive { get; set; }

    public string? Ballast { get; set; }

    public string? OrderNumber { get; set; }

    public string? Item { get; set; }

    public int? AfterWash { get; set; }
    public string? Cycle { get; set; }
}
