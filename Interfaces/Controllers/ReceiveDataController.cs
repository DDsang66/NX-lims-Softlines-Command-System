using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Interfaces;
using NX_lims_Softlines_Command_System.Application.Tools;
using System.Diagnostics;
using System.IO.Compression;



namespace NX_lims_Softlines_Command_System.Interfaces.Controllers
{
    [ApiController]
    [Route("api/receivedata")]
    public class ReceiveDataController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ExcelHelper _excel;
        private readonly IPrintExcelStrategyFactory _factory;
        public ReceiveDataController(IWebHostEnvironment env, ExcelHelper excel, IPrintExcelStrategyFactory factory)
        {
            _env = env;
            _excel = excel;
            _factory = factory;
        }

        [HttpPost("showExcel")]
        public async Task<IActionResult> ShowExcel([FromBody] ExcelSubmitDto dto)
        {
            ReceiveDataHelper helper = new ReceiveDataHelper(_excel, _env, _factory);
            var (wetOut, phyOut) = await helper.Helper(dto);
            var files = new[] { wetOut, phyOut }.Where(System.IO.File.Exists).ToList();
            if (!files.Any())
                return BadRequest("无可下载的文件");

            var zipPath = Path.Combine(_env.WebRootPath, "ExcelModel/SavingExcel",
                                       $"Report_{Guid.NewGuid():N}.zip");

            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var f in files)
                    archive.CreateEntryFromFile(f!, Path.GetFileName(f)!);
            }

            if (!System.IO.File.Exists(zipPath))
                return BadRequest("生成的 ZIP 文件不存在");

            // 读取文件到内存流
            var memoryStream = new MemoryStream();
            using (var fileStream = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
            {
                await fileStream.CopyToAsync(memoryStream);
            }
            memoryStream.Position = 0;

            var fileSize = memoryStream.Length;
            Console.WriteLine($"Generated ZIP file size: {fileSize} bytes");
            // 返回文件流
            var contentType = "application/zip";
            string? filename = null;
            // 注册回调，在响应完成后删除文件
            Response.RegisterForDispose(new DeleteFileOnDispose(zipPath));
            Response.Headers["Content-Disposition"] = $"attachment; filename=\"{"DataSheet_"}+{dto.ReportNumber}\"";

            Console.WriteLine($"Content-Disposition: attachment; filename=\"{"DataSheet_"}+{dto.ReportNumber}\"");
            return File(memoryStream, contentType, filename);
        }
    }
}
