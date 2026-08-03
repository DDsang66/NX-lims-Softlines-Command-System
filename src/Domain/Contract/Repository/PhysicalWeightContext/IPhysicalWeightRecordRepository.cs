using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.PhysicalWeightContext;

/// <summary>物理称重记录仓储接口 (IScopedDependency 自动注册)</summary>
public interface IPhysicalWeightRecordRepository
{
    Task<PhysicalWeightRecord?> GetByIdAsync(Guid id);
    Task<List<PhysicalWeightRecord>> GetByReportNumberAsync(string reportNumber);
    Task AddAsync(PhysicalWeightRecord record, CancellationToken ct);
    Task AddBatchAsync(List<PhysicalWeightRecord> records, CancellationToken ct);
    Task<bool> DeleteAsync(Guid id);
    Task<int> DeleteBatchAsync(List<Guid> ids);
}
