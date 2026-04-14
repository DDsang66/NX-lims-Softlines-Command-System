using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class BasicParam
{
    public long IdParam { get; set; }

    public string IndexItem { get; set; } = null!;

    public string IndexStandardCode { get; set; } = null!;

    public string IndexSmapleInfo { get; set; } = null!;

    public string TestGroup { get; set; } = null!;

    public string Param { get; set; } = null!;
}
