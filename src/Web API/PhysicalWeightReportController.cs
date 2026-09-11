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
        string? filePath = ResolveReportFile(fileName);
        if (filePath == null)
            return NotFound(new { success = false, message = "文件不存在" });

        return PhysicalFile(
            filePath,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            fileDownloadName: fileName,
            enableRangeProcessing: true
        );
    }

    /// <summary>
    /// 定位报告文件。新报告按月存 wwwroot/DocxModel/SaveDocx/Weight{yyyyMM}/(与生成侧同步),
    /// 老报告仍在直存 SaveDocx/ 根目录, 还要兼容"上月底生成、本月初才点下载"的跨月情况,
    /// 所以按 当月目录 → 根目录 → 各子目录扫一遍 的顺序找。
    /// </summary>
    private string? ResolveReportFile(string fileName)
    {
        // 文件名必须是纯文件名 —— 本方法直接把路由参数拼进磁盘路径, 先校验再拼, 挡住路径穿越
        if (string.IsNullOrWhiteSpace(fileName) ||
            !string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal))
            return null;

        string root = Path.Combine(_env.WebRootPath, "DocxModel", "SaveDocx");

        string monthly = Path.Combine(root, "Weight" + DateTime.Now.ToString("yyyyMM"), fileName);
        if (System.IO.File.Exists(monthly)) return monthly;

        string legacy = Path.Combine(root, fileName);
        if (System.IO.File.Exists(legacy)) return legacy;

        return Directory.Exists(root)
            ? Directory.EnumerateFiles(root, fileName, SearchOption.AllDirectories).FirstOrDefault()
            : null;
    }
}
