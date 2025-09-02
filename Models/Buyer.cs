using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NX_lims_Softlines_Command_System.Models;

public partial class Buyer
{
    [Key]
    public long Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public string? PhoneNum { get; set; }

    public string? Mail { get; set; }

    public int? ManualHash { get; set; }

    public string? Manual { get; set; }

    public string? ContactPerson { get; set; }
}
