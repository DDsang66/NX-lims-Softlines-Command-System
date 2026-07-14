using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class SampleInfo
{
    public string IdSample { get; set; } = null!;

    public string ReportNumber { get; set; } = null!;

    public string SampleCode { get; set; } = null!;

    public string? Remark { get; set; }
}
