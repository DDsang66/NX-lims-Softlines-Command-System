using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class BasicParamStructure
{
    public string ParamStructureId { get; set; } = null!;

    public string ParamName { get; set; } = null!;

    public string? Schema { get; set; }

    public string? AllowedValue { get; set; }

    public byte Status { get; set; }

    public DateTime EffectiveDate { get; set; }

    public byte? EngineLayer { get; set; }

    public string? FormulaId { get; set; }

    public virtual ICollection<ParamsturctureStandardfamily> ParamsturctureStandardfamilies { get; set; } = new List<ParamsturctureStandardfamily>();
}
