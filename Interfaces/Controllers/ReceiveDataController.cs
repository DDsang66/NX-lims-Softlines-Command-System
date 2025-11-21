using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;
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
                return StatusCode(500, new { success = false, message = "无可下载的文件" });

            var zipPath = Path.Combine(_env.WebRootPath, "ExcelModel/SavingExcel",
                                       $"Report_{Guid.NewGuid():N}.zip");

            using (var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var f in files)
                    archive.CreateEntryFromFile(f!, Path.GetFileName(f)!);
            }

            if (!System.IO.File.Exists(zipPath))
                return StatusCode(500, new { success = false, message = "生成的 ZIP 文件不存在" });

            // 读取文件到内存流
            var memoryStream = new MemoryStream();
            using (var fileStream = new FileStream(zipPath, FileMode.Open, FileAccess.Read))
            {
                await fileStream.CopyToAsync(memoryStream);
            }
            memoryStream.Position = 0;

            var fileSize = memoryStream.Length;
            // 返回文件流
            // 注册回调，在响应完成后删除文件
            Response.RegisterForDispose(new DeleteFileOnDispose(zipPath));
            //Response.Headers["Content-Disposition"] = $"attachment; filename=\"{"DataSheet_"}+{dto.ReportNumber}\"";
            var filename = $"DataSheet_{dto.ReportNumber}.zip";   // 不要加号
            return File(memoryStream, "application/zip", filename);
        }

        #region
        //[HttpPost("showExcel")]
        //public async Task<IActionResult> ShowExcel([FromBody] ExcelSubmitDto dto)
        //{
        //    // 1. 并行生成两份 Excel（内存流）
        //    var helper = new ReceiveDataHelper(_excel, _env, _factory);
        //    var (wetStream, phyStream) = await helper.GenerateAsync(dto); // 返回 MemoryStream

        //    if (wetStream == null && phyStream == null)
        //        return StatusCode(500, new { success = false, message = "无可下载的文件" });

        //    // 2. 内存里直接打 Zip（无临时文件）
        //    var zipStream = new MemoryStream();
        //    using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        //    {
        //        if (wetStream != null)
        //        {
        //            wetStream.Position = 0;
        //            var entry = archive.CreateEntry("WetReport.xlsx", CompressionLevel.Optimal);
        //            using var entryStream = entry.Open();
        //            await wetStream.CopyToAsync(entryStream);
        //        }
        //        if (phyStream != null)
        //        {
        //            phyStream.Position = 0;
        //            var entry = archive.CreateEntry("PhyReport.xlsx", CompressionLevel.Optimal);
        //            using var entryStream = entry.Open();
        //            await phyStream.CopyToAsync(entryStream);
        //        }
        //    }

        //    zipStream.Position = 0;
        //    return File(zipStream, "application/zip", $"DataSheet_{dto.ReportNumber}.zip");
        //}
        #endregion
    }
}
