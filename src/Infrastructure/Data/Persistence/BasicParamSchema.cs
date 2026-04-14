using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class BasicParamSchema
{
    public string IdSchema { get; set; } = null!;

    public string ParamterName { get; set; } = null!;

    public string? Unit { get; set; }

    public string Type { get; set; } = null!;

    public string? AllowedValue { get; set; }

    public long? StandardFamilyCodeId { get; set; }
}
