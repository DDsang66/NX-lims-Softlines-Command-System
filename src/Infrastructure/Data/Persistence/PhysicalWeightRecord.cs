using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class PhysicalWeightRecord
{
    public Guid Id { get; set; }

    public int RecordIndex { get; set; }

    public string? TestPoint { get; set; }

    public decimal Weight { get; set; }

    public decimal Area { get; set; }

    public decimal GPerSqm { get; set; }

    public decimal OzPerSqyd { get; set; }

    public decimal? EnvTemperature { get; set; }

    public decimal? EnvHumidity { get; set; }

    public DateTime TestTime { get; set; }

    public string? ReportNumber { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? SampleId { get; set; }

    public string? TestType { get; set; }

    public decimal? LengthCm { get; set; }

    public int? PieceCount { get; set; }

    public decimal GPerM { get; set; }

    public decimal OzPerYd { get; set; }

    public decimal GPerPiece { get; set; }

    public decimal LbPerDozen { get; set; }
}
