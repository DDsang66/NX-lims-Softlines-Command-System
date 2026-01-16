using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class NormalParameter
{
    public string ParamId { get; set; } = null!;

    public string? ContactItem { get; set; }

    public string? ContactSample { get; set; }

    public string? ReportNumber { get; set; }

    public string? Cycle { get; set; }

    public string? Load { get; set; }

    public string? CleanseProcedure { get; set; }

    public string? WashNum { get; set; }

    public string? Pressure { get; set; }

    public string? ExtraParam { get; set; }

    public string? Remark { get; set; }
}
