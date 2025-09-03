using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class Composition
{
    public int FiberId { get; set; }

    public string? FiberName { get; set; }

    public string? FiberSource { get; set; }

    public string? FiberType { get; set; }

    public string? FiberDescripe { get; set; }
}
