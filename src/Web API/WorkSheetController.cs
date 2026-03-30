using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Service;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/worksheet")]
    public class WorkSheetController : ControllerBase
    {
        private readonly ExcelAppService _excelAppService;

        public WorkSheetController(ExcelAppService excelAppService)
        {
            _excelAppService = excelAppService;
        }

        [HttpGet("excelurl")]
        public async Task<IActionResult> GetExcelUrl(string repo, string buyer, string group)
        {
            // 1. 参数校验（可以移到 FluentValidation）
            if (string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(buyer) || string.IsNullOrEmpty(group))
                return BadRequest(new { success = false, message = "参数不能为空" });

            // 2. 调用应用服务获取结果
            var result = await _excelAppService.GetExcelAccessInfoAsync(repo, buyer, group);

            // 3. 审计（可以在 ActionFilter 中做，或者在这里显式调用）
            //await _auditService.LogAsync("ExcelAccess", new { repo, buyer, group, result.FileKey });

            // 4. 返回
            return result.IsSuccess
                   ? Ok(result)
                   : BadRequest(new { message = result.Error });
        }

        [HttpGet("{repoNum}/{fileName}/download")]
        public  IActionResult Download(string fileName,string repoNum)
        {
            var filePath =  _excelAppService.GetExcelFilePathAsync(fileName,repoNum);
            // 返回文件流，Content-Type 必须正确
            return PhysicalFile(
                filePath,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileDownloadName: fileName,
                enableRangeProcessing: true  // 支持断点续传
            );
        }
    }
}
