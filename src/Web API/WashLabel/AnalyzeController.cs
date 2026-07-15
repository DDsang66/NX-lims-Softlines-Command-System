using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Interface.WashLabel;

namespace NX_lims_Softlines_Command_System.src.Web_API.WashLabel;

[ApiController]
[Route("api/[controller]")]
public class WashLabelController : ControllerBase
{
    private readonly IWashLabelAnalysisService _service;

    public WashLabelController(IWashLabelAnalysisService service)
    {
        _service = service;
    }

    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "请上传一张图片" });

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { error = "图片大小不能超过10MB" });

        var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(new { error = "仅支持 JPG、PNG、GIF、WebP 格式的图片" });

        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            var imageBytes = ms.ToArray();

            var result = await _service.AnalyzeImageAsync(imageBytes, file.ContentType);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { error = "AI 服务配置错误：" + ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, new { error = "AI API 调用失败：" + ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "分析失败：" + ex.Message });
        }
    }
}
