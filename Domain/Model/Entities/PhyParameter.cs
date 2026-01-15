using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class PhyParameter
{
    public string ParamId { get; set; } = null!;

    public string? ContactItem { get; set; }

    public string? ContactSample { get; set; }

    public string? ReportNumber { get; set; }

    public string? IsAfterwash { get; set; }

    public string? WashingContactParam { get; set; }

    public string? Revolution { get; set; }

    public string? LoadValue { get; set; }

    public string? LoadUnit { get; set; }

    public string? Pressure { get; set; }

    public string? Remark { get; set; }
}
