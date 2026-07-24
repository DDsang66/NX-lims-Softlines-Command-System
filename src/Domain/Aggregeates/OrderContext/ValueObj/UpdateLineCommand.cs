using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.Enums;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;

/// <summary>
/// 更新订单行的参数对象
/// </summary>
public sealed record UpdateLineCommand(
    OrderExpress? Express = null,
    DateTimeOffset? DueDate = null,
    DateTimeOffset? LabIn = null,
    int? SampleCount = null,
    int? ItemCount = null,
    string? Reviewer = null,
    string? Remark = null,
    string? DelayType = null,
    string? DelayReason = null
);
