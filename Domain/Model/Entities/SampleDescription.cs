using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class SampleDescription
{
    public int IdSampleDescription { get; set; }

    public string PropertyName { get; set; } = null!;

    public string? PropertyValue { get; set; }

    public string Type { get; set; } = null!;

    public string BuyerName { get; set; } = null!;

    public string? DefaultValue { get; set; }
}
