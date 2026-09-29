using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.DataSheetConetxt;
using NX_lims_Softlines_Command_System.src.Application.Service.DataSheetContext;
using NX_lims_Softlines_Command_System.src.Application.UseCase;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Web_API.UseCase
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataEditorController : ControllerBase
    {
        private readonly DataSheetQueryService _dataSheetQueryService;
        private readonly DataSheetService _dataSheetService;    
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<DataEditorController> _logger;


        public DataEditorController(
            DataSheetQueryService dataSheetQueryService, 
            DataSheetService dataSheetService,
            IWebHostEnvironment env,
            ILogger<DataEditorController> logger)
        {
            _logger = logger;
            _dataSheetService = dataSheetService;
            _dataSheetQueryService = dataSheetQueryService;
            _env = env;
        }

        [HttpGet("get/{checklistId}")]
        public async Task<Result<List<DataSheetResponseDto>>> GenerateCheckList(Guid checklistId, CancellationToken ct)
        {
            var result = await _dataSheetQueryService.GetDataSheetByChecklistId(checklistId, ct);

            return result;
        }


        /// <summary>
        /// 下载地址
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        [HttpGet("datasheet/download/{*url}")]
        public IActionResult Download(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return BadRequest(new { success = false, message = "url 不能为空" });

            // ★ 统一为正斜杠，去掉前导斜杠/反斜杠
            url = url.Replace('\\', '/').TrimStart('/', '\\');

            if (url.Contains(".."))
                return BadRequest(new { success = false, message = "非法路径" });

            // 拼到 WebRoot 下
            var filePath = Path.Combine(
                _env.WebRootPath,
                url.Replace('/', Path.DirectorySeparatorChar)
            );

            // 越界保护
            var fullPath = Path.GetFullPath(filePath);
            var webRootFull = Path.GetFullPath(_env.WebRootPath);
            if (!fullPath.StartsWith(webRootFull, StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { success = false, message = "路径越界" });

            if (!System.IO.File.Exists(fullPath))
            {
                return NotFound(new
                {
                    success = false,
                    message = "文件不存在",
                    triedPath = fullPath          // ★ 方便你在浏览器里核对
                });
            }

            var extension = Path.GetExtension(url).ToLowerInvariant();
            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                _ => "application/octet-stream"
            };

            return PhysicalFile(fullPath, contentType, Path.GetFileName(url), enableRangeProcessing: true);
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
                    await _dataSheetService.SaveFromOnlyOffice(datasheetId, payload.Url, ct);
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
