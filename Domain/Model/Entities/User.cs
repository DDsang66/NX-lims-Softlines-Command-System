using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class User
{
    [Key]
    public string UserId { get; set; } = null!;

    public string? UserName { get; set; }

    public string? PassWord { get; set; }

    public string? NickName { get; set; }

    public string? EmployeeId { get; set; }
}
