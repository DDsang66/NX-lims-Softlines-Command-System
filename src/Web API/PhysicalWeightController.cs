using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API;

/// <summary>物理称重记录 API — /api/PhysicalWeight</summary>
[ApiController]
[Route("api/[Controller]")]
public class PhysicalWeightController : ControllerBase
{
    private readonly IPhysicalWeightRecordService _svc;

    public PhysicalWeightController(IPhysicalWeightRecordService svc) { _svc = svc; }

    /// <summary>批量保存称重记录</summary>
    [HttpPost]
    public Task<Result<List<PhysicalWeightOutputDto>>> Save([FromBody] PhysicalWeightSaveRequestDto r, CancellationToken ct)
        => _svc.SaveRecordsAsync(r, ct);

    /// <summary>按报告号查询记录列表</summary>
    [HttpGet]
    public Task<Result<List<PhysicalWeightOutputDto>>> Query([FromQuery] string reportNumber, CancellationToken ct)
        => _svc.GetRecordsAsync(reportNumber, ct);

    /// <summary>删除单条记录</summary>
    [HttpDelete("{id:guid}")]
    public Task<Result> Delete(Guid id, CancellationToken ct)
        => _svc.DeleteAsync(id, ct);

    /// <summary>批量删除记录</summary>
    [HttpDelete("batch")]
    public Task<Result<int>> DeleteBatch([FromBody] PhysicalWeightBatchDeleteDto dto, CancellationToken ct)
        => _svc.DeleteBatchAsync(dto, ct);
}
