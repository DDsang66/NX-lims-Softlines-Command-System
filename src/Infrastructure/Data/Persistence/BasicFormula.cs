using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class BasicFormula
{
    public string FormulaId { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string ParamName { get; set; } = null!;

    public string? ConditionFields { get; set; }

    public string? ExpressionTemplate { get; set; }

    public string? Description { get; set; }

    public int? Version { get; set; }

    public DateTime? EffectiveDate { get; set; }

    public bool IsActive { get; set; }

    public byte? EngineLayer { get; set; }

    public virtual ICollection<FormulaStandardfamily> FormulaStandardfamilies { get; set; } = new List<FormulaStandardfamily>();
}
