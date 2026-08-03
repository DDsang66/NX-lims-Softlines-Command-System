using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;

/// <summary>前端提交的单条称重记录</summary>
public class PhysicalWeightInputDto
{
    public int RecordIndex { get; set; }
    public string? SampleId { get; set; }
    public string? TestPoint { get; set; }
    public decimal Weight { get; set; }
    public decimal Area { get; set; }
    public decimal GPerSqm { get; set; }
    public decimal OzPerSqyd { get; set; }
    public decimal? EnvTemperature { get; set; }
    public decimal? EnvHumidity { get; set; }
    public DateTime TestTime { get; set; }
    public string? ReportNumber { get; set; }
}

/// <summary>返回给前端的称重记录</summary>
public class PhysicalWeightOutputDto
{
    public Guid Id { get; set; }
    public int RecordIndex { get; set; }
    public string? SampleId { get; set; }
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
}

/// <summary>批量删除请求</summary>
public class PhysicalWeightBatchDeleteDto
{
    public List<Guid> Ids { get; set; } = new();
}

/// <summary>批量保存请求</summary>
public class PhysicalWeightSaveRequestDto
{
    public List<PhysicalWeightInputDto> Records { get; set; } = new();
}
