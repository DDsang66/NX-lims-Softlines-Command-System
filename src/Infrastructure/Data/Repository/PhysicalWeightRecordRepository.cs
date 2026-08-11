// ============================================================
// 物理称重记录仓储 (PhysicalWeightRecordRepository)
//
// 职责: DDD 分层中的"持久化边界" —— 只负责把领域聚合根
//       (Domain.PhysicalWeightRecord) 转换为 EF 持久化实体
//       (Persistence.PhysicalWeightRecord, 即物理表映射) 并执行增删查,
//       不包含任何业务规则。
//
// 关键约定:
//   - 写入只做 Add/Remove 登记, 不调用 SaveChanges —— 由上层
//     IUnitOfWork 统一提交, 保证多个仓储的改动在同一事务里落库。
//   - 读取一律 AsNoTracking(不跟踪), 因为领域层自行管理状态。
//   - 入库用 Mapster 的 Adapt, 出库用私有 Map(Reconstitute),
//     映射配置集中在 PhysicalWeightMappingConfig.cs。
//
// 命名说明: 领域聚合根与持久化实体同名 PhysicalWeightRecord, 分属不同
// 命名空间。文件底部 using 别名把裸名 PhysicalWeightRecord 指向领域类,
// 持久化实体则以全限定名出现, 避免歧义。
// ============================================================
using Mapster;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.PhysicalWeightContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;
using PhysicalWeightRecord = NX_lims_Softlines_Command_System.src.Domain.Aggregeates.PhysicalWeightContext.PhysicalWeightRecord;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository;

/// <summary>
/// 物理称重记录仓储实现 — Domain↔持久化实体映射, 不自调 SaveChanges(由 IUnitOfWork 统一提交)。
/// 实现 Domain.Contract 里的仓储接口, 通过 IScopedDependency 注册为作用域依赖(每请求一个实例)。
/// </summary>
public class PhysicalWeightRecordRepository : IPhysicalWeightRecordRepository, IScopedDependency
{
    private readonly dbContext _context;

    public PhysicalWeightRecordRepository(dbContext context) { _context = context; }

    /// <summary>按聚合根 Id 查单条记录, 查不到返回 null。AsNoTracking: 只读查询不跟踪, 省内存且避免意外脏写。</summary>
    public async Task<PhysicalWeightRecord?> GetByIdAsync(PhysicalWeightRecordId id, CancellationToken ct)
    {
        var po = await _context.PhysicalWeightRecords.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        return po == null ? null : Map(po);
    }

    /// <summary>按报告号查该报告的全部记录(按 RecordIndex 升序), 供报告生成/回显使用。</summary>
    public async Task<List<src.Domain.Aggregeates.PhysicalWeightContext.PhysicalWeightRecord>> GetByReportNumberAsync(string reportNumber, CancellationToken ct)
        => (await _context.PhysicalWeightRecords.AsNoTracking()
            .Where(r => r.ReportNumber == reportNumber)
            .OrderBy(r => r.RecordIndex)
            .ToListAsync(ct)).Select(Map).ToList();

    /// <summary>登记新增单条记录。Adapt 把领域聚合根平铺成持久化实体; 不落库, 等 IUnitOfWork.SaveAsync 统一提交。</summary>
    public async Task AddAsync(src.Domain.Aggregeates.PhysicalWeightContext.PhysicalWeightRecord record, CancellationToken ct)
        => await _context.PhysicalWeightRecords.AddAsync(record.Adapt<src.Infrastructure.Data.Persistence.PhysicalWeightRecord>(), ct);

    /// <summary>登记批量新增(一次测试可能产生多条记录)。映射与单条相同, 交给 EF AddRange 批量跟踪。</summary>
    public async Task AddRangeAsync(IEnumerable<src.Domain.Aggregeates.PhysicalWeightContext.PhysicalWeightRecord> records, CancellationToken ct)
        => await _context.PhysicalWeightRecords.AddRangeAsync(records.Adapt<List<src.Infrastructure.Data.Persistence.PhysicalWeightRecord>>(), ct);

    /// <summary>按 Id 删除单条。先查出实体再 Remove(EF 需要已跟踪实体才能删); 不存在返回 false。</summary>
    public async Task<bool> DeleteAsync(PhysicalWeightRecordId id, CancellationToken ct)
    {
        var po = await _context.PhysicalWeightRecords.FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        if (po == null) return false;
        _context.PhysicalWeightRecords.Remove(po);
        return true;
    }

    /// <summary>按 Id 列表批量删除(如报告重做时清空旧记录)。返回实际删除条数。</summary>
    public async Task<int> DeleteByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct)
    {
        var list = ids.ToList();
        var pos = await _context.PhysicalWeightRecords.Where(r => list.Contains(r.Id)).ToListAsync(ct);
        if (pos.Count == 0) return 0;
        _context.PhysicalWeightRecords.RemoveRange(pos);
        return pos.Count;
    }

    /// <summary>持久化实体 → 领域聚合根(反向组装)。用 Reconstitute 而不是 Create:
    /// Create 会重置 CreatedAt 且用于"新建"; 读库回来的历史值应原样还原, 故走 Reconstitute。</summary>
    private static PhysicalWeightRecord Map(src.Infrastructure.Data.Persistence.PhysicalWeightRecord po) => PhysicalWeightRecord.Reconstitute(
        new PhysicalWeightRecordId(po.Id), po.RecordIndex, po.SampleId, po.TestPoint,
        po.Weight, po.Area, po.GPerSqm, po.OzPerSqyd, po.TestType, po.LengthCm, po.PieceCount,
        po.GPerM, po.OzPerYd, po.GPerPiece, po.LbPerDozen,
        po.EnvTemperature, po.EnvHumidity,
        po.TestTime, po.ReportNumber, po.CreatedAt);
}
