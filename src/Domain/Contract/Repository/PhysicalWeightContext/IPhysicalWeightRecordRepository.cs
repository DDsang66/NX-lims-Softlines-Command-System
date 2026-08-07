using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.PhysicalWeightContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.PhysicalWeightContext;

/// <summary>物理称重记录仓储接口 (IScopedDependency 自动注册)</summary>
public interface IPhysicalWeightRecordRepository : IScopedDependency
{
    Task<PhysicalWeightRecord?> GetByIdAsync(PhysicalWeightRecordId id, CancellationToken ct);
    Task<List<PhysicalWeightRecord>> GetByReportNumberAsync(string reportNumber, CancellationToken ct);
    Task AddAsync(PhysicalWeightRecord record, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<PhysicalWeightRecord> records, CancellationToken ct);
    Task<bool> DeleteAsync(PhysicalWeightRecordId id, CancellationToken ct);
    Task<int> DeleteByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct);
}
