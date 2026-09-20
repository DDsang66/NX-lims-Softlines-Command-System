using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class DataSheet
{
    public Guid Id { get; set; }

    public Guid BatchId { get; set; }

    public Guid CheckListId { get; set; }

    public string TestItemId { get; set; } = null!;

    public string ReportNumber { get; set; } = null!;

    public string? Url { get; set; }

    public string ContactTemplateUrl { get; set; } = null!;

    public int Status { get; set; }

    public int ModelIndex { get; set; }

    public string? ModelKey { get; set; }

    public string? ModelSnapshot { get; set; }

    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }

    public string Version { get; set; } = null!;

    public DateTime CreateTime { get; set; }

    public DateTime? UpdateTime { get; set; }

    public virtual DataSheetBatch Batch { get; set; } = null!;

    public virtual CheckList CheckList { get; set; } = null!;
}
