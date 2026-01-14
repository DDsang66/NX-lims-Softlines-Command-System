using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class SampleInfoDescription
{
    public string IdDescription { get; set; } = null!;

    public string SampleId { get; set; } = null!;

    public string? PropertyName { get; set; }

    public string? PropertyValue { get; set; }
}
