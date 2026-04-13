using Microsoft.AspNetCore.Mvc;

namespace NX_lims_Softlines_Command_System.Interfaces.Controllers
{
    [ApiController]
    [Route("api/fiber")]
    public class FiberDocxController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public FiberDocxController(IWebHostEnvironment env)
        {
            _env = env;
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
