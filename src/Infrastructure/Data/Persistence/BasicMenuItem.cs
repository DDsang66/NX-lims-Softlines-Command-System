using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class BasicMenuItem
{
    public Guid Id { get; set; }

    public string? TestItemId { get; set; }

    public string? StandardId { get; set; }

    public string? BuyerOwnName { get; set; }

    public string? BuyerModifiedTestItem { get; set; }

    public string? BuyerModifiedTestMethod { get; set; }

    public string? Requirement { get; set; }

    public string? BuyerModifiedGroup { get; set; }

    public string MenuId { get; set; } = null!;
}
