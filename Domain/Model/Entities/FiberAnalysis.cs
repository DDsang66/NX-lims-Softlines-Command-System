using System;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class FiberAnalysis
{
    public long Id { get; set; }

    public string? ReportNumber { get; set; }

    public string? Method { get; set; }

    public byte? Type { get; set; }

    public string? Buyer { get; set; }

    public string? FiberAnalysis1 { get; set; }

    public string? Remark { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}
