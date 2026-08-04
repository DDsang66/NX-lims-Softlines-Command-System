using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Application.Curd;

/// <summary>物理称重记录应用服务</summary>
public class PhysicalWeightRecordService : IScopedDependency
{
    private readonly IPhysicalWeightRecordRepository _repo;

    public PhysicalWeightRecordService(IPhysicalWeightRecordRepository repo) { _repo = repo; }

    /// <summary>批量保存 — DTO 映射为实体后写入数据库</summary>
    public async Task<object> SaveRecordsAsync(PhysicalWeightSaveRequestDto req, CancellationToken ct)
    {
        if (req.Records == null || req.Records.Count == 0)
            return new { success = false, message = "记录列表为空" };

        var entities = req.Records.Select(dto => new PhysicalWeightRecord
        {
            Id = Guid.NewGuid(),
            RecordIndex = dto.RecordIndex,
            TestPoint = dto.TestPoint, Weight = dto.Weight, Area = dto.Area,
            GPerSqm = dto.GPerSqm, OzPerSqyd = dto.OzPerSqyd,
            EnvTemperature = dto.EnvTemperature, EnvHumidity = dto.EnvHumidity,
            TestTime = dto.TestTime, ReportNumber = dto.ReportNumber,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        await _repo.AddBatchAsync(entities, ct);
        var outputs = entities.Select(e => e.Adapt<PhysicalWeightOutputDto>()).ToList();
        return new { success = true, message = "保存成功", data = outputs };
    }

    /// <summary>按报告号查询</summary>
    public async Task<object> GetRecordsAsync(string reportNumber)
    {
        var entities = await _repo.GetByReportNumberAsync(reportNumber);
        var outputs = entities.Select(e => e.Adapt<PhysicalWeightOutputDto>()).ToList();
        return new { success = true, message = "查询成功", data = outputs };
    }

    /// <summary>删除单条</summary>
    public async Task<object> DeleteAsync(Guid id)
    {
        var ok = await _repo.DeleteAsync(id);
        return ok ? new { success = true, message = "删除成功" }
                  : new { success = false, message = "记录不存在" };
    }

    /// <summary>批量删除</summary>
    public async Task<object> DeleteBatchAsync(PhysicalWeightBatchDeleteDto dto)
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
            return new { success = false, message = "ID 列表为空" };
        var count = await _repo.DeleteBatchAsync(dto.Ids);
        return new { success = true, message = $"成功删除 {count} 条", data = count };
    }
}
