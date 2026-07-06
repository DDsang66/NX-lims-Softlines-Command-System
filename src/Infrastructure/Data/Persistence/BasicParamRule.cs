using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class BasicParamRule
{
    public string RuleId { get; set; } = null!;

    public string? ParamName { get; set; }

    public string? ConditionPattern { get; set; }

    public string? DefaultValue { get; set; }

    public int Priority { get; set; }

    public bool StopOnMatch { get; set; }

    public bool IsActive { get; set; }

    public string StandardFamilyCodeId { get; set; } = null!;

    public string FormulaId { get; set; } = null!;

    public string? ParamStructureId { get; set; }
}
