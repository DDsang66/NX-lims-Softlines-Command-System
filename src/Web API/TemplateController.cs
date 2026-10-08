using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.DataSheetConetxt;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TemplateContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.TemplateContext;
using NX_lims_Softlines_Command_System.src.Application.Service.DataSheetContext;
using NX_lims_Softlines_Command_System.src.Application.Service.TemplateContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Web_API.UseCase;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[controller]")]
    public class TemplateController : ControllerBase
    {
        private readonly ITemplateAppService _templateAppService;
        private readonly ITemplateQueryService _templateQueryService;
        private readonly ILogger<TemplateController> _logger;
        private readonly IWebHostEnvironment _env;

        public TemplateController(
            ITemplateAppService templateAppService, 
            ITemplateQueryService templateQueryService,
            ILogger<TemplateController> logger,
            IWebHostEnvironment env)
        {
            _templateAppService = templateAppService;
            _templateQueryService = templateQueryService;
            _logger = logger;
            _env = env;
        }

        [HttpPost("add")]
        public async Task<Result> AddTemplate([FromForm] AddTemplateDto dto, CancellationToken ct)
        {
            var result = await _templateAppService.CreateTemplateAsync(dto, ct);

            return result;
        }

        [HttpPut("update")]
        public async Task<Result> UpdateTemplate([FromForm] UpdateTemplateDto dto, CancellationToken ct)
        {
            var result = await _templateAppService.UpdateTemplateAsync(dto, ct);

            return result;
        }

        /// <summary>
        /// 发布模板
        /// </summary>
        /// <param name="templateId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPut("publish/{templateId}")]
        public async Task<Result> PublishTemplate(string templateId, CancellationToken ct) 
        {
            var result = await _templateAppService.TemplatePublishAsync(templateId, ct);

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

        /// <summary>
        /// OnlyOffice 保存回调：编辑器把最新文档 POST 到这里
        /// </summary>
        [HttpPost("onlyoffice/callback")]
        public async Task<IActionResult> OnlyOfficeCallback(
            [FromQuery] string datasheetId,
            CancellationToken ct)
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync(ct);
                _logger.LogInformation("OnlyOffice callback body: {Body}", body);

                var payload = JsonSerializer.Deserialize<OnlyOfficeCallbackDto>(
                    body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation("callback status={Status}, url={Url}",
                    payload?.Status, payload?.Url);

                if ((payload?.Status == 2 || payload?.Status == 6)
                    && !string.IsNullOrEmpty(payload.Url))
                {
                    //await _dataSheetService.SaveFromOnlyOffice(datasheetId, payload.Url, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OnlyOffice callback error");
                // ★ 即使保存失败，也要返回 error:0 让 DS 解锁
                // 否则编辑器会一直只读
            }

            // ★ 必须返回
            return Ok(new { error = 0 });
        }


    }
}
