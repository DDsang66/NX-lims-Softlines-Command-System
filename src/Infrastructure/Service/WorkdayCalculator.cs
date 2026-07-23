using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.OrderContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Service;

/// <summary>
/// 工作日计算器 — 排除周末和法定节假日。
/// 假期数据从 holiday 表（NX-lims Lab Command Sys）读取。
/// </summary>
public class WorkdayCalculator : IWorkdayCalculator, IScopedDependency
{
    private readonly IHolidayRepository _holidayRepo;

    public WorkdayCalculator(IHolidayRepository holidayRepo)
    {
        _holidayRepo = holidayRepo;
    }

    /// <summary>
    /// 计算 start（含）到 end（含）之间的工作日天数。
    /// 规则：跳过周六日 + 跳过法定节假日(is_makeup=0)，但调休工作日(is_makeup=1)即便周末也计数。
    /// </summary>
    public async Task<int> GetWorkdaysAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct = default)
    {
        var holidays = await _holidayRepo.GetHolidaysAsync(start.Year, ct);
        var makeupDays = await _holidayRepo.GetMakeupWorkdaysAsync(start.Year, ct);
        if (end.Year != start.Year)
        {
            holidays.UnionWith(await _holidayRepo.GetHolidaysAsync(end.Year, ct));
            makeupDays.UnionWith(await _holidayRepo.GetMakeupWorkdaysAsync(end.Year, ct));
        }

        int days = 0;
        var d = start.Date;
        var last = end.Date;
        while (d <= last)
        {
            // 跳过普通假期（非调休）
            if (holidays.Contains(d))
            {
                d = d.AddDays(1);
                continue;
            }
            // 调休日直接算工作日，即使周末
            if (makeupDays.Contains(d))
            {
                days++;
                d = d.AddDays(1);
                continue;
            }
            // 正常工作日（跳过周末）
            if (d.DayOfWeek != DayOfWeek.Saturday
                && d.DayOfWeek != DayOfWeek.Sunday)
            {
                days++;
            }
            d = d.AddDays(1);
        }
        return days;
    }

    /// <summary>
    /// 根据 labIn（进实验室）和 dueDate（截止日期）之间的工作日天数，计算急单等级。
    /// ≤1 天 = SameDay | ≤2 天 = Shuttle | ≤3 天 = Express | 其余 = Regular
    /// </summary>
    public async Task<OrderExpress> ComputeExpressAsync(DateTimeOffset labIn, DateTimeOffset dueDate, CancellationToken ct = default)
    {
        var days = await GetWorkdaysAsync(labIn, dueDate, ct);
        return days switch
        {
            <= 1 => OrderExpress.SameDay,
            <= 2 => OrderExpress.Shuttle,
            <= 3 => OrderExpress.Express,
            _   => OrderExpress.Regular
        };
    }
}
