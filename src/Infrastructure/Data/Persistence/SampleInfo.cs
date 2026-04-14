using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class SampleInfo
{
    public string IdSample { get; set; } = null!;

    public string ReportNumber { get; set; } = null!;

    public string SampleCode { get; set; } = null!;

    public long? IndexComposition { get; set; }

    public long? IndexCarelabel { get; set; }

    public string? Structure { get; set; }

    public string? ApparelLocation { get; set; }

    public string? SampleDescription { get; set; }

    public string? Remark { get; set; }
}
