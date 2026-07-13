using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class BasicParamStructure
{
    public string ParamStructureId { get; set; } = null!;

    public string ParamName { get; set; } = null!;

    public string? Schema { get; set; }

    public string? AllowedValue { get; set; }

    public DateTime EffectiveDate { get; set; }

    public virtual ICollection<ParamstructureFormula> ParamstructureFormulas { get; set; } = new List<ParamstructureFormula>();

    public virtual ICollection<ParamsturctureStandardfamily> ParamsturctureStandardfamilies { get; set; } = new List<ParamsturctureStandardfamily>();
}
