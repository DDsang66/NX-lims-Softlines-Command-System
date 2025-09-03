using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class UserProfile
{
    public string EmployeeId { get; set; } = null!;

    public string? RealName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Gender { get; set; }

    public DateOnly? Birth { get; set; }

    public string? IdCard { get; set; }
}
