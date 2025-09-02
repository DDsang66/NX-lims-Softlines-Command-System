using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class Composition
{
    [Key]
    [Column("fiber_id")]
    public int FiberId { get; set; }
    [Column("fiber_name")]
    public string? FiberName { get; set; }
    [Column("fiber_source")]
    public string? FiberSource { get; set; }
    [Column("fiber_type")]
    public string? FiberType { get; set; }
    [Column("fiber_descripe")]
    public string? FiberDescripe { get; set; }
}
