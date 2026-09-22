using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TemplateContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.TemplateContext;
using NX_lims_Softlines_Command_System.src.Application.Service.TemplateContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[controller]")]
    public class TemplateController : ControllerBase
    {
        private readonly ITemplateAppService _templateAppService;
        private readonly ITemplateQueryService _templateQueryService;
        private readonly IWebHostEnvironment _env;

        public TemplateController(
            ITemplateAppService templateAppService, 
            ITemplateQueryService templateQueryService,
            IWebHostEnvironment env)
        {
            _templateAppService = templateAppService;
            _templateQueryService = templateQueryService;
            _env = env;
        }

        [HttpPost("add")]
        public async Task<Result> AddTemplate([FromForm] AddTemplateDto dto, CancellationToken ct)
        {
            var result = await _templateAppService.CreateTemplateAsync(dto, ct);

            return result;
        }

        [HttpGet("getall")]
        public async Task<Result<List<TemplateResponseDto>>> GetAllTemplateAsync(CancellationToken ct) 
        {
            var result = await _templateQueryService.GetAllTemplateAsync(ct);

            return result;
        }

        [HttpGet("download/{*fileUrl}")]
        public IActionResult Download(string fileUrl)
        {
            var filePath = Path.Combine(_env.WebRootPath, fileUrl);
            if (!System.IO.File.Exists(filePath))
                return NotFound(new { success = false, message = "文件不存在" });

            var extension = Path.GetExtension(fileUrl).ToLowerInvariant();
            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };

            var fileName = Path.GetFileName(fileUrl);   // ★ 提取文件名

            return PhysicalFile(
                filePath,
                contentType,
                fileDownloadName: fileName,
                enableRangeProcessing: true
            );
        }
    }
}
