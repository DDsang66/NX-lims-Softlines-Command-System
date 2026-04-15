using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Mvc;
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

        [HttpGet("get-docxUrl")]
        public IActionResult Index()
        {
            var fileName = "FIBER_ANALYSIS_DATA_SHEET.docx";
            var filePath = Path.Combine(_env.WebRootPath, "DocxModel", fileName);
            return PhysicalFile(
                filePath,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileDownloadName: fileName,
                enableRangeProcessing: true  // 支持断点续传
            );
        }
    }
}
