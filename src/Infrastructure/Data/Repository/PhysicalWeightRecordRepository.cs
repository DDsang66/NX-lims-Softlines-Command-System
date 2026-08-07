using Mapster;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.PhysicalWeightContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository;

/// <summary>物理称重记录仓储实现 — Domain↔PO 映射, 不自调 SaveChanges(由 IUnitOfWork 统一提交)</summary>
public class PhysicalWeightRecordRepository : IPhysicalWeightRecordRepository, IScopedDependency
{
    private readonly dbContext _context;

    public PhysicalWeightRecordRepository(dbContext context) { _context = context; }

    public async Task<PhysicalWeightRecord?> GetByIdAsync(PhysicalWeightRecordId id, CancellationToken ct)
    {
        var po = await _context.PhysicalWeightRecords.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return po == null ? null : Map(po);
    }

    public async Task<List<PhysicalWeightRecord>> GetByReportNumberAsync(string reportNumber, CancellationToken ct)
        => (await _context.PhysicalWeightRecords.AsNoTracking()
            .Where(r => r.ReportNumber == reportNumber)
            .OrderBy(r => r.RecordIndex)
            .ToListAsync(ct)).Select(Map).ToList();

    public async Task AddAsync(PhysicalWeightRecord record, CancellationToken ct)
        => await _context.PhysicalWeightRecords.AddAsync(record.Adapt<PhysicalWeightRecordPo>(), ct);

    public async Task AddRangeAsync(IEnumerable<PhysicalWeightRecord> records, CancellationToken ct)
        => await _context.PhysicalWeightRecords.AddRangeAsync(records.Adapt<List<PhysicalWeightRecordPo>>(), ct);

    public async Task<bool> DeleteAsync(PhysicalWeightRecordId id, CancellationToken ct)
    {
        var po = await _context.PhysicalWeightRecords.FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        if (po == null) return false;
        _context.PhysicalWeightRecords.Remove(po);
        return true;
    }

    public async Task<int> DeleteByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var list = ids.ToList();
        var pos = await _context.PhysicalWeightRecords.Where(r => list.Contains(r.Id)).ToListAsync(ct);
        if (pos.Count == 0) return 0;
        _context.PhysicalWeightRecords.RemoveRange(pos);
        return pos.Count;
    }

    private static PhysicalWeightRecord Map(PhysicalWeightRecordPo po) => PhysicalWeightRecord.Reconstitute(
        new PhysicalWeightRecordId(po.Id), po.RecordIndex, po.SampleId, po.TestPoint,
        po.Weight, po.Area, po.Gsm, po.Oz, po.TestType, po.LengthCm, po.PieceCount,
        po.GPerM, po.OzPerYd, po.GPerPiece, po.LbPerDozen,
        po.EnvTemperature, po.EnvHumidity,
        po.TestTime, po.ReportNumber, po.CreatedAt);
}
