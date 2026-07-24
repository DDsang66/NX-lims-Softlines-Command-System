namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.OrderContext;

/// <summary>
/// 进单请求 — 前端 OrderEntry 页面点 Confirm 时提交的数据
/// </summary>
public class AddOrderRequest
{
    /// <summary>订单行列表（一个 ReportNumber 对应多个 Group）</summary>
    public List<AddOrderLineInput>? Rows { get; set; }

    /// <summary>整单备注</summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 进单明细行 — 每个 Group 一行
/// </summary>
public class AddOrderLineInput
{
    /// <summary>报告编号（如 87.405.26.0001.01）</summary>
    public string? ReportNumber { get; set; }

    /// <summary>进单人 ID</summary>
    public string? OrderEntryPerson { get; set; }

    /// <summary>CS（客服）ID</summary>
    public int? CustomerServiceId { get; set; }

    /// <summary>测试组：Physics / Wet / Fiber / Flam</summary>
    public string? TestGroup { get; set; }

    /// <summary>要求完成日期</summary>
    public DateTimeOffset? DueDate { get; set; }

    /// <summary>进实验室时间</summary>
    public DateTimeOffset? LabIn { get; set; }

    /// <summary>单行备注</summary>
    public string? Remark { get; set; }

    /// <summary>RFID 编码</summary>
    public string? RfidCode { get; set; }
}
