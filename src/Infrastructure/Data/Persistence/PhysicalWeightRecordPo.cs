using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

/// <summary>物理称重记录持久化模型 (EF PO)</summary>
public partial class PhysicalWeightRecordPo
{
    public Guid Id { get; set; }

    public int RecordIndex { get; set; }

    /// <summary>试样编号</summary>
    public string? SampleId { get; set; }

    public string? TestPoint { get; set; }

    public decimal Weight { get; set; }

    public decimal Area { get; set; }

    public decimal Gsm { get; set; }

    public decimal Oz { get; set; }

    /// <summary>测试类型: area | length | piece</summary>
    public string? TestType { get; set; }

    /// <summary>试样长度 cm(长度克重用)</summary>
    public decimal? LengthCm { get; set; }

    /// <summary>条数(条重用)</summary>
    public int? PieceCount { get; set; }

    /// <summary>长度克重 g/m</summary>
    public decimal GPerM { get; set; }

    /// <summary>长度克重 oz/yd</summary>
    public decimal OzPerYd { get; set; }

    /// <summary>条重 g/piece</summary>
    public decimal GPerPiece { get; set; }

    /// <summary>条重 lb/dozen</summary>
    public decimal LbPerDozen { get; set; }

    public decimal? EnvTemperature { get; set; }

    public decimal? EnvHumidity { get; set; }

    public DateTime TestTime { get; set; }

    public string? ReportNumber { get; set; }

    public DateTime CreatedAt { get; set; }
}
