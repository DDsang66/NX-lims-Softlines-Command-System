using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class CheckListItem
{
    public Guid CheckListItemId { get; set; }

    public string? BuyerModifiedTestItem { get; set; }

    public string? BuyerModifiedTestStandard { get; set; }

    public byte TestGroup { get; set; }

    public string? TestPointParams { get; set; }

    public string Samples { get; set; } = null!;

    public byte Status { get; set; }

    public string? TestItemId { get; set; }

    public string? StandardId { get; set; }

    public Guid CheckListId { get; set; }
}
