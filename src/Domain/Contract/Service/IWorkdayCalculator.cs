using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.Enums;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service;

/// <summary>
/// 工作日计算合同 — 排除周末和法定节假日。
/// 实现在 Infrastructure 层。
/// </summary>
public interface IWorkdayCalculator
{
    /// <summary>计算 start 到 end 之间的工作日天数</summary>
    Task<int> GetWorkdaysAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct = default);

    /// <summary>计算急单等级 — ≤1天 SameDay | ≤2天 Shuttle | ≤3天 Express | 其余 Regular</summary>
    Task<OrderExpress> ComputeExpressAsync(DateTimeOffset labIn, DateTimeOffset dueDate, CancellationToken ct = default);
}
