using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Service;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine;

namespace NX_lims_Softlines_Command_System.Interfaces.Controllers
{
    [ApiController]
    [Route("api/fiberdocx")]
    public class FiberDocxController : ControllerBase,IScopedDependency
    {
        private readonly IWebHostEnvironment _env;
        private readonly WordTemplateEngine _templateEngine = new WordTemplateEngine();

        public FiberDocxController(IWebHostEnvironment env, WordTemplateEngine templateEngine)
        {
            _env = env;
            _templateEngine = templateEngine;
        }

        /// <summary>
        /// 下发成分报告**模板**本身，供录入页右侧 OnlyOffice 预览。
        /// </summary>
        /// <param name="type">
        /// 组分类型：只认 <c>Single</c>（大小写不敏感），其余（含 null）一律按多组分。
        /// </param>
        /// <remarks>
        /// ⚠️ <paramref name="type"/> 的值会参与 <see cref="Path.Combine(string, string, string)"/>，
        /// 所以**只能走白名单**、绝不接受任意字符串 —— 否则就是一个目录穿越入口。
        /// 文件名取自 <see cref="FiberWorksheetService.TemplateOf"/>（与生成侧同源），
        /// 别在这儿另抄一份文件名常量：模板改名时两边会悄悄走散。
        /// </remarks>
        [HttpGet("get-docxUrl")]
        public IActionResult Index([FromQuery] string? type = null)
        {
            var analysisType = string.Equals(type, "Single", StringComparison.OrdinalIgnoreCase)
                ? AnalysisType.Single
                : AnalysisType.Multiple;

            var (dir, fileName) = FiberWorksheetService.TemplateOf(analysisType);
            var filePath = Path.Combine(_env.WebRootPath, "DocxModel", dir, fileName);
            return PhysicalFile(
                filePath,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileDownloadName: fileName,
                enableRangeProcessing: true  // 支持断点续传
            );
        }
    }
}
