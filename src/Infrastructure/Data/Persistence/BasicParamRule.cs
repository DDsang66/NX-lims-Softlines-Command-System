using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class BasicParamRule
{
    public string IdRule { get; set; } = null!;

    public string? Name { get; set; }

    public string? Formula { get; set; }

    public string? Mapping { get; set; }

    public string? DefaultValue { get; set; }

    public long? StandardFamilyCodeId { get; set; }

    public string Status { get; set; } = null!;
}
