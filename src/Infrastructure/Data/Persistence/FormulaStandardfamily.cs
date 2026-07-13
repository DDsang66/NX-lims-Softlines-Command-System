using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class FormulaStandardfamily
{
    public int Id { get; set; }

    public string FormulaId { get; set; } = null!;

    public string IdStandardFamily { get; set; } = null!;

    public virtual BasicFormula Formula { get; set; } = null!;

    public virtual BasicStandardFamily IdStandardFamilyNavigation { get; set; } = null!;
}
