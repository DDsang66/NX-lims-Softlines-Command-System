using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class AdidasMethodItemMap
{
    public int MethodId { get; set; }

    public string? MethodCode { get; set; }

    public string? MethodName { get; set; }

    public string? Type { get; set; }
}
