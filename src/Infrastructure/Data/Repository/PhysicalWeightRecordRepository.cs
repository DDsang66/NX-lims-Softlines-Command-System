using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository;

/// <summary>物理称重记录仓储实现 (注入新架构 dbContext)</summary>
public class PhysicalWeightRecordRepository : IPhysicalWeightRecordRepository, IScopedDependency
{
    private readonly dbContext _context;

    public PhysicalWeightRecordRepository(dbContext context) { _context = context; }

    public async Task<PhysicalWeightRecord?> GetByIdAsync(Guid id)
        => await _context.PhysicalWeightRecords.FindAsync(id);

    public async Task<List<PhysicalWeightRecord>> GetByReportNumberAsync(string reportNumber)
        => await _context.PhysicalWeightRecords
            .Where(r => r.ReportNumber == reportNumber)
            .OrderBy(r => r.RecordIndex)
            .ToListAsync();

    public async Task AddAsync(PhysicalWeightRecord record, CancellationToken ct)
    {
        await _context.PhysicalWeightRecords.AddAsync(record, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task AddBatchAsync(List<PhysicalWeightRecord> records, CancellationToken ct)
    {
        await _context.PhysicalWeightRecords.AddRangeAsync(records, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await _context.PhysicalWeightRecords.FindAsync(id);
        if (entity == null) return false;
        _context.PhysicalWeightRecords.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> DeleteBatchAsync(List<Guid> ids)
    {
        var entities = await _context.PhysicalWeightRecords
            .Where(r => ids.Contains(r.Id)).ToListAsync();
        if (entities.Count == 0) return 0;
        _context.PhysicalWeightRecords.RemoveRange(entities);
        await _context.SaveChangesAsync();
        return entities.Count;
    }
}
