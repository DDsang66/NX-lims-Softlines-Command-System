using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class BasicStandardFamily
{
    public string IdStandardFamily { get; set; } = null!;

    public string StandardFamilyCode { get; set; } = null!;

    public int Version { get; set; }

    public DateTime EffectiveDate { get; set; }
}
