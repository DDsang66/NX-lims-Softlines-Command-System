namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.OrderContext;

/// <summary>
/// 订单列表输出 — 一个 ReportNumber 对应一条记录，包含多个 Lines（Group）
/// </summary>
public class OrderOutput
{
    /// <summary>报告编号</summary>
    public string? ReportNumber { get; set; }

    /// <summary>进单人姓名</summary>
    public string? OrderEntryPerson { get; set; }

    /// <summary>进单人 ID</summary>
    public string? OrderEntryPersonId { get; set; }

    /// <summary>客服名称</summary>
    public string? CustomerServiceName { get; set; }

    /// <summary>客服 ID</summary>
    public int? CustomerServiceId { get; set; }

    /// <summary>测试组列表（逗号分隔，如 "Physics,Wet,Fiber"）</summary>
    public string? TestGroups { get; set; }

    /// <summary>该订单下的所有 Group 行</summary>
    public List<OrderLineOutput>? Lines { get; set; } = new();
}

/// <summary>
/// 订单行输出 — 对应一个 Group 的详细信息
/// </summary>
public class OrderLineOutput
{
    /// <summary>行主键（对应 domain LineId）</summary>
    public string? LineId { get; set; }

    /// <summary>快递类型：Regular / Express / Shuttle / Same Day</summary>
    public string? Express { get; set; }

    /// <summary>测试组：Physics / Wet / Fiber / Flam</summary>
    public string? TestGroup { get; set; }

    /// <summary>测点数量</summary>
    public int SampleCount { get; set; }

    /// <summary>测试项目数量</summary>
    public int ItemCount { get; set; }

    /// <summary>行备注</summary>
    public string? Remark { get; set; }

    /// <summary>审单人姓名</summary>
    public string? Reviewer { get; set; }

    /// <summary>审单人 ID</summary>
    public string? ReviewerId { get; set; }

    /// <summary>延迟类型</summary>
    public string? DelayType { get; set; }

    /// <summary>延迟原因</summary>
    public string? DelayReason { get; set; }

    /// <summary>审单完成时间</summary>
    public DateTimeOffset? ReviewFinish { get; set; }

    /// <summary>进入实验室时间</summary>
    public DateTimeOffset? LabIn { get; set; }

    /// <summary>要求完成日期</summary>
    public DateTimeOffset? DueDate { get; set; }

    /// <summary>出实验室时间</summary>
    public DateTimeOffset? LabOut { get; set; }

    /// <summary>RFID 电子标签码</summary>
    public string? RfidCode { get; set; }

    /// <summary>状态：EntryComplete / ReviewComplete / InLab / TestDone / ReportOut</summary>
    public string? Status { get; set; }
}

/// <summary>
/// 订单汇总视图 — 前端 OrderSummary 页面使用，一行代表一个 Group
/// </summary>
public class OrderSummary
{
    /// <summary>行主键</summary>
    public string? LineId { get; set; }

    /// <summary>报告编号</summary>
    public string? ReportNumber { get; set; }

    /// <summary>进单人姓名</summary>
    public string? OrderEntryPerson { get; set; }

    /// <summary>进单人 ID</summary>
    public string? OrderEntryPersonId { get; set; }

    /// <summary>快递类型</summary>
    public string? Express { get; set; }

    /// <summary>客服名称</summary>
    public string? CustomerServiceName { get; set; }

    /// <summary>客服 ID</summary>
    public int? CustomerServiceId { get; set; }

    /// <summary>测试组</summary>
    public string? TestGroup { get; set; }

    /// <summary>延迟类型</summary>
    public string? DelayType { get; set; }

    /// <summary>延迟原因</summary>
    public string? DelayReason { get; set; }

    /// <summary>审单完成时间</summary>
    public DateTimeOffset? ReviewFinish { get; set; }

    /// <summary>审单人姓名</summary>
    public string? Reviewer { get; set; }

    /// <summary>审单人 ID</summary>
    public string? ReviewerId { get; set; }

    /// <summary>要求完成日期</summary>
    public DateTimeOffset? DueDate { get; set; }

    /// <summary>进入实验室时间</summary>
    public DateTimeOffset? LabIn { get; set; }

    /// <summary>出实验室时间</summary>
    public DateTimeOffset? LabOut { get; set; }

    /// <summary>测点数量</summary>
    public int SampleCount { get; set; }

    /// <summary>测试项目数量</summary>
    public int ItemCount { get; set; }

    /// <summary>RFID 电子标签码</summary>
    public string? RfidCode { get; set; }

    /// <summary>行备注</summary>
    public string? Remark { get; set; }

    /// <summary>状态</summary>
    public string? Status { get; set; }
}

/// <summary>
/// 分页结果 — 泛型，T 为每行数据类型
/// </summary>
/// <typeparam name="T">行数据类型</typeparam>
public sealed class PageResult<T>
{
    /// <summary>当前页数据</summary>
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>总记录数</summary>
    public int TotalCount { get; init; }

    /// <summary>当前页码（1-based）</summary>
    public int Page { get; init; }

    /// <summary>每页条数</summary>
    public int PageSize { get; init; }

    /// <summary>是否有上一页</summary>
    public bool HasPrevious => Page > 1;

    /// <summary>是否有下一页</summary>
    public bool HasNext => Page * PageSize < TotalCount;
}
