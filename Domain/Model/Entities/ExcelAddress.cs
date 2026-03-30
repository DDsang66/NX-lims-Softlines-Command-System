using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

public partial class ExcelAddress
{
    public long IdExcelAddress { get; set; }

    public string ReportNumber { get; set; } = null!;

    public string Address { get; set; } = null!;

    public string Status { get; set; } = null!;

    public byte[] RowVersion { get; set; } = null!;
}
