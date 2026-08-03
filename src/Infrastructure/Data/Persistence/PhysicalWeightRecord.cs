// 物理称重记录持久化实体 → 映射表 physical_weight_record
// g/m² = Weight / Area × 10000  |  oz/yd² = g/m² / 33.9057

using System;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

public partial class PhysicalWeightRecord
{
    public Guid Id { get; set; }
    public int RecordIndex { get; set; }          // 序号
    public string? SampleId { get; set; }          // 试样编号
    public string? TestPoint { get; set; }         // 试样测点
    public decimal Weight { get; set; }            // 重量(g)
    public decimal Area { get; set; }              // 面积(cm²)
    public decimal GPerSqm { get; set; }           // g/m²
    public decimal OzPerSqyd { get; set; }         // oz/yd²
    public decimal? EnvTemperature { get; set; }   // 环境温度(℃)
    public decimal? EnvHumidity { get; set; }      // 环境湿度(%)
    public DateTime TestTime { get; set; }         // 测试时间
    public string? ReportNumber { get; set; }      // 关联报告号
    public DateTime CreatedAt { get; set; }        // 创建时间
}
