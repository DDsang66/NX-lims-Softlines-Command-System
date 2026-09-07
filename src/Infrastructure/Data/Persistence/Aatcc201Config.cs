using System;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

/// <summary>
/// AATCC 201 校准参数持久化实体（对应物理表 aatcc201_config）。
/// 纯数据载体，属性名与 dbContext OnModelCreating 里的列映射一一对应。
/// 领域逻辑在 Domain.Aggregeates.MoistureDryingRateContext.Aatcc201Config，
/// 这里只是 EF Core 落库用的 PO（仓储在两者间做 Mapster 映射）。
/// </summary>
public partial class Aatcc201Config
{
    /// <summary>主键（ValueGeneratedNever）</summary>
    public Guid Id { get; set; }

    /// <summary>设备编号</summary>
    public string MachineNo { get; set; } = string.Empty;

    /// <summary>表面温度1偏置(℃)</summary>
    public decimal TempHw1 { get; set; }

    /// <summary>表面温度2偏置(℃)</summary>
    public decimal TempHw2 { get; set; }

    /// <summary>加热板温度1偏置(℃)</summary>
    public decimal TempBoard1 { get; set; }

    /// <summary>加热板温度2偏置(℃)</summary>
    public decimal TempBoard2 { get; set; }

    /// <summary>风速1偏置(m/s)</summary>
    public decimal Wind1 { get; set; }

    /// <summary>风速2偏置(m/s)</summary>
    public decimal Wind2 { get; set; }

    /// <summary>斜坡段判定点数</summary>
    public int SlopePoint { get; set; }

    /// <summary>平缓段判定点数</summary>
    public int FlatPoint { get; set; }

    /// <summary>斜坡判定序号</summary>
    public int SlopeDgNo { get; set; }

    /// <summary>斜坡持续点数</summary>
    public int SlopeContinueNo { get; set; }

    /// <summary>斜坡持续温差(℃)</summary>
    public decimal SlopeContinueTemp { get; set; }

    /// <summary>设定温度(℃)</summary>
    public decimal SetTemp { get; set; }

    /// <summary>加热板1修正(℃)</summary>
    public decimal TempBoard1X { get; set; }

    /// <summary>加热板2修正(℃)</summary>
    public decimal TempBoard2X { get; set; }

    /// <summary>PID 比例系数</summary>
    public decimal P { get; set; }

    /// <summary>PID 积分系数</summary>
    public decimal I { get; set; }

    /// <summary>PID 微分系数</summary>
    public decimal D { get; set; }

    /// <summary>最近更新时间</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>最近更新人</summary>
    public string? UpdatedBy { get; set; }
}
