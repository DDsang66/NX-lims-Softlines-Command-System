using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class Permission
{
    public int PermissionIndex { get; set; }

    public string? Review { get; set; }

    public string? ReviewWet { get; set; }

    public string? ReviewPhysics { get; set; }

    public string? Role { get; set; }
}
