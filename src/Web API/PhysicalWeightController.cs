using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Curd;

namespace NX_lims_Softlines_Command_System.src.Web_API;

/// <summary>物理称重记录 API — /api/PhysicalWeight</summary>
[ApiController]
[Route("api/[Controller]")]
public class PhysicalWeightController : ControllerBase
{
    private readonly PhysicalWeightRecordService _svc;
    public PhysicalWeightController(PhysicalWeightRecordService svc) { _svc = svc; }

    /// <summary>批量保存称重记录</summary>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] PhysicalWeightSaveRequestDto r, CancellationToken ct)
        => Ok(await _svc.SaveRecordsAsync(r, ct));

    /// <summary>按报告号查询记录列表</summary>
    [HttpGet]
    public async Task<IActionResult> Query([FromQuery] string reportNumber)
        => Ok(await _svc.GetRecordsAsync(reportNumber));

    /// <summary>删除单条记录</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
        => Ok(await _svc.DeleteAsync(id));

    /// <summary>批量删除记录</summary>
    [HttpDelete("batch")]
    public async Task<IActionResult> DeleteBatch([FromBody] PhysicalWeightBatchDeleteDto dto)
        => Ok(await _svc.DeleteBatchAsync(dto));
}
