using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class ParamstructureFormula
{
    public int Id { get; set; }

    public string ParamStructureId { get; set; } = null!;

    public string FormulaId { get; set; } = null!;

    public virtual BasicFormula Formula { get; set; } = null!;

    public virtual BasicParamStructure ParamStructure { get; set; } = null!;
}
