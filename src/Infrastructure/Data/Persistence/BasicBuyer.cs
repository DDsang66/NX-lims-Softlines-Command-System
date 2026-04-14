using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class BasicBuyer
{
    public string BuyerCode { get; set; } = null!;

    public string BuyerName { get; set; } = null!;

    public string? Remark { get; set; }

    public int? SampleStorageDate { get; set; }

    public string? Country { get; set; }

    public byte IsIndividualTraveler { get; set; }
}
