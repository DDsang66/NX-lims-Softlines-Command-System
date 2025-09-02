using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class AdidasMethodItemMap
{
    [Key]
    public int MethodId { get; set; }

    public string? MethodCode { get; set; }

    public string? MethodName { get; set; }

    public string? Type { get; set; }
}
