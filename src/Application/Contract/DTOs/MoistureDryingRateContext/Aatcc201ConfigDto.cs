namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MoistureDryingRateContext;

/// <summary>
/// AATCC 201 校准参数输出 DTO（GET/PUT aatcc201-config 回给前端）。
/// 字段与领域聚合 Aatcc201Config 一一对应，供校准对话框回显。
/// </summary>
public class Aatcc201ConfigDto
{
    /// <summary>主键</summary>
    public Guid Id { get; init; }

    /// <summary>设备编号</summary>
    public string MachineNo { get; init; } = string.Empty;

    /// <summary>表面温度1偏置(℃)</summary>
    public decimal TempHw1 { get; init; }

    /// <summary>表面温度2偏置(℃)</summary>
    public decimal TempHw2 { get; init; }

    /// <summary>加热板温度1偏置(℃)</summary>
    public decimal TempBoard1 { get; init; }

    /// <summary>加热板温度2偏置(℃)</summary>
    public decimal TempBoard2 { get; init; }

    /// <summary>风速1偏置(m/s)</summary>
    public decimal Wind1 { get; init; }

    /// <summary>风速2偏置(m/s)</summary>
    public decimal Wind2 { get; init; }

    /// <summary>斜坡段判定点数</summary>
    public int SlopePoint { get; init; }

    /// <summary>平缓段判定点数</summary>
    public int FlatPoint { get; init; }

    /// <summary>斜坡判定序号</summary>
    public int SlopeDgNo { get; init; }

    /// <summary>斜坡持续点数</summary>
    public int SlopeContinueNo { get; init; }

    /// <summary>斜坡持续温差(℃)</summary>
    public decimal SlopeContinueTemp { get; init; }

    /// <summary>设定温度(℃)</summary>
    public decimal SetTemp { get; init; }

    /// <summary>加热板1修正(℃)</summary>
    public decimal TempBoard1X { get; init; }

    /// <summary>加热板2修正(℃)</summary>
    public decimal TempBoard2X { get; init; }

    /// <summary>PID 比例系数</summary>
    public decimal P { get; init; }

    /// <summary>PID 积分系数</summary>
    public decimal I { get; init; }

    /// <summary>PID 微分系数</summary>
    public decimal D { get; init; }

    /// <summary>最近更新时间</summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>最近更新人</summary>
    public string? UpdatedBy { get; init; }
}

/// <summary>
/// AATCC 201 校准参数保存请求 DTO（PUT aatcc201-config）。
/// 覆盖全部可调字段；UpdatedBy 为操作员名，入库时同时刷新 updated_at/updated_by。
/// </summary>
public class Aatcc201ConfigSaveDto
{
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

    /// <summary>更新人（操作员）</summary>
    public string? UpdatedBy { get; set; }
}
