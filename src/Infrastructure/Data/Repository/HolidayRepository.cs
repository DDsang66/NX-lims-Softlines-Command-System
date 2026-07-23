using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Domain.Model;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.OrderContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository;

/// <summary>
/// 法定节假日仓储 — 从 holiday 表查询指定年份的假期和调休日期。
/// is_makeup=0 → 假期（休息），is_makeup=1 → 调休（上班）。
/// 每年手动维护一次当年数据。
/// </summary>
public class HolidayRepository : IHolidayRepository, IScopedDependency
{
    private readonly LabDbContextSec _context;

    public HolidayRepository(LabDbContextSec context)
    {
        _context = context;
    }

    public async Task<HashSet<DateTime>> GetHolidaysAsync(int year, CancellationToken ct)
    {
        var start = new DateTime(year, 1, 1);
        var end = new DateTime(year + 1, 1, 1);
        return await _context.Holidays
            .AsNoTracking()
            .Where(h => h.Date >= start && h.Date < end && !h.IsMakeup)
            .Select(h => h.Date)
            .ToHashSetAsync(ct);
    }

    public async Task<HashSet<DateTime>> GetMakeupWorkdaysAsync(int year, CancellationToken ct)
    {
        var start = new DateTime(year, 1, 1);
        var end = new DateTime(year + 1, 1, 1);
        return await _context.Holidays
            .AsNoTracking()
            .Where(h => h.Date >= start && h.Date < end && h.IsMakeup)
            .Select(h => h.Date)
            .ToHashSetAsync(ct);
    }
}
