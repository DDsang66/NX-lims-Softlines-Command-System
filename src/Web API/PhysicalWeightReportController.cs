using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API;

/// <summary>物理克重报告 API — 生成 docx / 下载</summary>
[ApiController]
[Route("api/[Controller]")]
public class PhysicalWeightReportController : ControllerBase
{
    private readonly IPhysicalWeightReportService _reportService;
    private readonly IWebHostEnvironment _env;

    public PhysicalWeightReportController(IPhysicalWeightReportService reportService, IWebHostEnvironment env)
    {
        _reportService = reportService;
        _env = env;
    }

    /// <summary>生成物理克重报告 docx</summary>
    [HttpPost("report")]
    public Result<DocxUrlResponseDto> Generate([FromBody] PhysicalWeightReportRequestDto dto)
        => _reportService.Generate(dto);

    /// <summary>下载生成的 docx</summary>
    [HttpGet("{fileName}/download")]
    public IActionResult Download(string fileName)
    {
        var filePath = Path.Combine(_env.WebRootPath, "DocxModel", "SaveDocx", fileName);
        if (!System.IO.File.Exists(filePath))
            return NotFound(new { success = false, message = "文件不存在" });

        return PhysicalFile(
            filePath,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            fileDownloadName: fileName,
            enableRangeProcessing: true
        );
    }
}
