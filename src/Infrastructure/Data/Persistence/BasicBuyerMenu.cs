using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class BasicBuyerMenu
{
    public string MenuId { get; set; } = null!;

    public string MenuName { get; set; } = null!;

    public string? Remark { get; set; }

    public byte Status { get; set; }

    public DateTime UploadTime { get; set; }

    public string BuyerCode { get; set; } = null!;
}
