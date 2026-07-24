using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class CheckList
{
    public Guid CheckListId { get; set; }

    public string? Remark { get; set; }

    public DateTime CreatedTime { get; set; }

    public byte Status { get; set; }

    public string OrderId { get; set; }
}
