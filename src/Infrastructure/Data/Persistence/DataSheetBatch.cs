using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class DataSheetBatch
{
    public Guid Id { get; set; }

    public Guid CheckListId { get; set; }

    public string ReportNo { get; set; } = null!;

    public int Total { get; set; }

    public int Status { get; set; }

    public string TemplateUrl { get; set; } = null!;

    public string? MergedPdfUrl { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual CheckList CheckList { get; set; } = null!;

    public virtual ICollection<DataSheet> DataSheets { get; set; } = new List<DataSheet>();
}
