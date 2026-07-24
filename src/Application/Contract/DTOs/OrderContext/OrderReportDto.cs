namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.OrderContext;

/// <summary>
/// 订单卡片统计 — 前端 OrderReporting 仪表盘使用
/// </summary>
public class OrderCardOutput
{
    /// <summary>应出实验室数</summary>
    public int? NeedLabOut { get; set; }

    /// <summary>实际出实验室数</summary>
    public int? ActuallyLabOut { get; set; }

    /// <summary>延迟出实验室数</summary>
    public int? DelayLabOut { get; set; }

    /// <summary>提前出实验室数</summary>
    public int? InAdvanceLabOut { get; set; }

    /// <summary>测点总数</summary>
    public int? NumOfSample { get; set; }

    /// <summary>内部原因延迟数</summary>
    public int? InternalReasonDelay { get; set; }
}

/// <summary>
/// 饼图数据 — 订单时效占比
/// </summary>
public class OrderFanCardOutput
{
    /// <summary>延迟数</summary>
    public int? Delay { get; set; }

    /// <summary>提前数</summary>
    public int? InAdvance { get; set; }

    /// <summary>正常数</summary>
    public int? Normal { get; set; }

    /// <summary>在时效内数</summary>
    public int? InDueDate { get; set; }

    /// <summary>未知数</summary>
    public int? Unknown { get; set; }

    /// <summary>内部原因延迟数</summary>
    public int? InternalReasonDelay { get; set; }
}

/// <summary>
/// 折线图数据 — 订单时效趋势
/// </summary>
public class OrderLineCardOutput
{
    /// <summary>时间轴标签（如月份列表）</summary>
    public int[]? TimePropertyName { get; set; }

    /// <summary>多条折线的数据</summary>
    public List<TimePropertyValue>? TimeProperty { get; set; }
}

/// <summary>
/// 折线图中单条折线的值
/// </summary>
public class TimePropertyValue
{
    /// <summary>折线名称（如 "Delay", "On Time"）</summary>
    public string? TimeHead { get; set; }

    /// <summary>折线数据点</summary>
    public int[]? TimeValue { get; set; }
}
