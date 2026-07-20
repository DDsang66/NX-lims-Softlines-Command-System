using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class BasicParamRule
{
    public string RuleId { get; set; } = null!;

    public string ParamName { get; set; } = null!;

    public string ConditionPattern { get; set; } = null!;

    public string DefaultValue { get; set; } = null!;

    public int Priority { get; set; }

    public bool StopOnMatch { get; set; }

    public bool IsActive { get; set; }

    public string? ParamStructureId { get; set; }

    public string? FormulaId { get; set; }
}
