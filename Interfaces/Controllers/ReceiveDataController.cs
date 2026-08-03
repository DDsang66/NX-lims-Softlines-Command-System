using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService.ExcelPrintTool;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;
using NX_lims_Softlines_Command_System.Domain.Model;
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
        private LabDbContextSec _db;
        public ReceiveDataController(IWebHostEnvironment env, ExcelHelper excel, IPrintExcelStrategyFactory factory, LabDbContextSec db)
        {
            _env = env;
            _excel = excel;
            _factory = factory;
            _db = db; 
        }

        [HttpPost("showExcel")]
        public async Task<IActionResult> ShowExcel([FromBody] ExcelSubmitDto dto)
        {
            ReceiveDataHelper helper = new ReceiveDataHelper(_excel, _env, _factory,_db);
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

            // 接收数据后自动将对应 ReportNumber 的单据状态更新为 ReviewComplete（2），
            // 标记该报告号下的实验数据已接收完毕，后续可直接进入实验室流程
            var lab = _db.LabTestInfos.FirstOrDefault(l => l.ReportNumber == dto.ReportNumber);

            if(lab !=null)
            {
                lab.Status = 2;
                _db.LabTestInfos.Update(lab);
                _db.SaveChanges();
            }



            Response.RegisterForDispose(new DeleteFileOnDispose(zipPath));
            var filename = $"DataSheet_{dto.ReportNumber}.zip";   // 不要加号
            return File(memoryStream, "application/zip", filename);
        }
    }
}
