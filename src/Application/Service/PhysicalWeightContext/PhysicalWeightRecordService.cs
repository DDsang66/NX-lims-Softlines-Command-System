using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.PhysicalWeightContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repositories;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.PhysicalWeightContext;

/// <summary>物理称重记录应用服务 — 依赖 Domain 抽象, 经 IUnitOfWork 统一提交</summary>
public class PhysicalWeightRecordService : IPhysicalWeightRecordService, IScopedDependency
{
    private readonly IPhysicalWeightRecordRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public PhysicalWeightRecordService(IPhysicalWeightRecordRepository repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    /// <summary>批量保存 — DTO 经 Mapster 工厂转为 Domain 聚合后入库</summary>
    public async Task<Result<List<PhysicalWeightOutputDto>>> SaveRecordsAsync(PhysicalWeightSaveRequestDto req, CancellationToken ct)
    {
        if (req.Records == null || req.Records.Count == 0)
            return Result<List<PhysicalWeightOutputDto>>.Fail("记录列表为空");

        var entities = req.Records.Select(dto => dto.Adapt<Domain.Aggregeates.PhysicalWeightContext.PhysicalWeightRecord>()).ToList();

        await _repo.AddRangeAsync(entities, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        var outputs = entities.Select(e => e.Adapt<PhysicalWeightOutputDto>()).ToList();
        return Result<List<PhysicalWeightOutputDto>>.Ok(outputs);
    }

    /// <summary>按报告号查询</summary>
    public async Task<Result<List<PhysicalWeightOutputDto>>> GetRecordsAsync(string reportNumber, CancellationToken ct)
    {
        var entities = await _repo.GetByReportNumberAsync(reportNumber, ct);
        var outputs = entities.Select(e => e.Adapt<PhysicalWeightOutputDto>()).ToList();
        return Result<List<PhysicalWeightOutputDto>>.Ok(outputs);
    }

    /// <summary>删除单条</summary>
    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var ok = await _repo.DeleteAsync(new PhysicalWeightRecordId(id), ct);
        if (!ok) return Result.Fail("记录不存在");
        await _unitOfWork.SaveChangesAsync(ct);
        return Result.Ok();
    }

    /// <summary>批量删除</summary>
    public async Task<Result<int>> DeleteBatchAsync(PhysicalWeightBatchDeleteDto dto, CancellationToken ct)
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
            return Result<int>.Fail("ID 列表为空");
        var count = await _repo.DeleteByIdsAsync(dto.Ids, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return Result<int>.Ok(count);
    }
}
