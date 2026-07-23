using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.OrderContext;

/// <summary>
/// 法定节假日查询合同 — 从 holiday 表读取假期和调休日期。
/// 表结构：holiday (date DATE PK, name NVARCHAR(100), is_makeup BIT)
/// 实现在 Infrastructure 层。
/// </summary>
public interface IHolidayRepository : IScopedDependency
{
    /// <summary>查询指定年份所有假期日期集合（is_makeup=0）</summary>
    Task<HashSet<DateTime>> GetHolidaysAsync(int year, CancellationToken ct);

    /// <summary>查询指定年份所有调休工作日集合（is_makeup=1）— 这些日期即使周末也算工作日</summary>
    Task<HashSet<DateTime>> GetMakeupWorkdaysAsync(int year, CancellationToken ct);
}
