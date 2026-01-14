using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class SampleInfo
{
    public string IdSample { get; set; } = null!;

    public string ReportNumber { get; set; } = null!;

    public string ContactBuyer { get; set; } = null!;

    public string SampleCode { get; set; } = null!;

    public string? DescriptionId { get; set; }

    public string? Remark { get; set; }
}
