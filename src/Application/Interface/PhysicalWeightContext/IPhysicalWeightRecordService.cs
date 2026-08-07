using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.PhysicalWeightContext;

/// <summary>物理称重记录应用服务接口</summary>
public interface IPhysicalWeightRecordService : IScopedDependency
{
    Task<Result<List<PhysicalWeightOutputDto>>> SaveRecordsAsync(PhysicalWeightSaveRequestDto req, CancellationToken ct);
    Task<Result<List<PhysicalWeightOutputDto>>> GetRecordsAsync(string reportNumber, CancellationToken ct);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct);
    Task<Result<int>> DeleteBatchAsync(PhysicalWeightBatchDeleteDto dto, CancellationToken ct);
}
