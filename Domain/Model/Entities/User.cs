using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class User
{
    public string UserId { get; set; } = null!;

    public string? UserName { get; set; }

    public string? PassWord { get; set; }

    public string? NickName { get; set; }

    public string? EmployeeId { get; set; }

    public int? PermissionIndex { get; set; }

    public byte? Status { get; set; }

    public DateTime? CreateTime { get; set; }

    public DateTime? UpdatedTime { get; set; }

    public int? LoginFailCount { get; set; }

    public string? LastLoginIp { get; set; }
}
