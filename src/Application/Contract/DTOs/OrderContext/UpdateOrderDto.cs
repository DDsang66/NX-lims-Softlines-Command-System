namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.OrderContext;

/// <summary>
/// 更新订单请求 — 前端 ReviewFinish / LabOut 页面提交
/// </summary>
public class UpdateOrderRequest
{
    /// <summary>待更新的行列表</summary>
    public List<UpdateOrderItem>? Rows { get; set; }
}

/// <summary>
/// 更新订单明细 — 一个 Group 行
/// </summary>
public class UpdateOrderItem
{
    /// <summary>行主键</summary>
    public string? LineId { get; set; }

    /// <summary>快递类型</summary>
    public string? Express { get; set; }

    /// <summary>进入实验室时间</summary>
    public DateTimeOffset? LabIn { get; set; }

    /// <summary>要求完成日期</summary>
    public DateTimeOffset? DueDate { get; set; }

    /// <summary>审单完成时间</summary>
    public DateTimeOffset? ReviewFinishTime { get; set; }

    /// <summary>出实验室时间</summary>
    public DateTimeOffset? LabOutTime { get; set; }

    /// <summary>测点数量</summary>
    public int? SampleCount { get; set; }

    /// <summary>测试项目数量</summary>
    public int? ItemCount { get; set; }

    /// <summary>审单人 ID</summary>
    public string? ReviewerId { get; set; }

    /// <summary>行备注</summary>
    public string? Remark { get; set; }

    /// <summary>延迟类型</summary>
    public string? DelayType { get; set; }

    /// <summary>延迟原因</summary>
    public string? DelayReason { get; set; }
}

/// <summary>
/// 删除行请求项
/// </summary>
/// <param name="LineId">行主键</param>
/// <param name="Reason">删除原因</param>
public record DeleteOrderItem(string LineId, string Reason);

/// <summary>
/// 删除订单请求 — 软删除指定行
/// </summary>
/// <param name="Items">待删除的行列表</param>
public record DeleteOrderRequest(IReadOnlyList<DeleteOrderItem> Items);
