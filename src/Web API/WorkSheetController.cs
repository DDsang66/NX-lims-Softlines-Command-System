using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Service;
using NX_lims_Softlines_Command_System.src.Domain.Share;

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

        /// <summary>
        /// 获取 Excel 文件访问信息
        /// </summary>
        /// <param name="repo"></param>
        /// <param name="buyer"></param>
        /// <param name="group"></param>
        /// <returns></returns>
        [HttpGet("excelurl")]

        public async Task<IActionResult> GetExcelUrl(string repo, string buyer, string group, CancellationToken ct)
        {
            // 1. 参数校验（可以移到 FluentValidation）
            if (string.IsNullOrEmpty(repo) || string.IsNullOrEmpty(buyer) || string.IsNullOrEmpty(group))
                return BadRequest(new { success = false, message = "参数不能为空" });

            // 2. 调用应用服务获取结果

            var result = await _excelAppService.GetExcelAccessInfoAsync(repo, buyer, group, ct);


            // 3. 审计（可以在 ActionFilter 中做，或者在这里显式调用）
            //await _auditService.LogAsync("ExcelAccess", new { repo, buyer, group, result.FileKey });

            // 4. 返回
            return result.IsSuccess
                   ? Ok(result)
                   : BadRequest(new { message = result.Error });
        }

        /// <summary>
        /// 下载 Excel 文件
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="repoNum"></param>
        /// <returns></returns>
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

        /// <summary>
        /// OnlyOffice 回调接口
        /// </summary>
        /// <returns></returns>
        [HttpPost("callback")]
        public async Task<IActionResult> Callback()
        {
            // 先读取请求体看看状态
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            // 简单记录日志，方便调试
            Console.WriteLine($"OnlyOffice Callback: {body}");

            // 必须返回 error: 0，否则 OnlyOffice 认为失败
            return Ok(new { error = 0 });
        }


        /// <summary>
        /// 接收 OnlyOffice saveAs 保存的文件
        /// </summary>
        [HttpPost("save-from-url")]

        public async Task<Result> SaveFromUrl([FromBody] SaveAsRequest request, CancellationToken ct)
        {
            var result = await _excelAppService.SaveAsExcelAccessInfoAsync(request,ct);

            return result.IsSuccess ?
                Result.Ok()
                : Result.Fail(result.Error);
        }
    }
}
