using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class Aatcc201Config
{
    public Guid Id { get; set; }

    public string MachineNo { get; set; } = null!;

    public decimal TempHw1 { get; set; }

    public decimal TempHw2 { get; set; }

    public decimal TempBoard1 { get; set; }

    public decimal TempBoard2 { get; set; }

    public decimal Wind1 { get; set; }

    public decimal Wind2 { get; set; }

    public int SlopePoint { get; set; }

    public int FlatPoint { get; set; }

    public int SlopeDgNo { get; set; }

    public int SlopeContinueNo { get; set; }

    public decimal SlopeContinueTemp { get; set; }

    public decimal SetTemp { get; set; }

    public decimal TempBoard1X { get; set; }

    public decimal TempBoard2X { get; set; }

    public decimal P { get; set; }

    public decimal I { get; set; }

    public decimal D { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }
}
