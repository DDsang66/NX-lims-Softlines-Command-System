using DocumentFormat.OpenXml.Office2021.DocumentTasks;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.LabScheduleContext;
using NX_lims_Softlines_Command_System.src.Application.UseCase;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API.UseCase
{
    [ApiController]
    [Route("api/[controller]")]
    public class LabScheduleController : ControllerBase
    {
        private readonly LabScheduleUseCaseService _useCase;
        private readonly IWebHostEnvironment _env;

        public LabScheduleController(LabScheduleUseCaseService useCase, IWebHostEnvironment env)
        {
            _env = env;
            _useCase = useCase;
        }

        /// <summary>
        /// 获取 LabSchedule 分页信息
        /// </summary>
        /// <param name="param">查询条件 + 分页参数</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>分页结果</returns>
        [HttpGet("page-result")]
        public async Task<Result<PagedResult<LabScheduleDto>>> GetLabSchedule(
            [FromQuery] CheckListQueryParamDto param,
            CancellationToken ct)
        {
            var result = await _useCase.GetSummaryAsync(param, ct);
            return Result<PagedResult<LabScheduleDto>>.Ok(result);
        }

        /// <summary>
        /// 触发合并并且获取合并后的下载链接
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPost("data-sheet-merge")]
        public async Task<Result<DocxUrlResponseDto>> DataSheetMerge(MergeLabScheduleDto dto, CancellationToken ct)
        {
            var result = await _useCase.HandleDataSheetMerge(dto, ct);

            return result;
        }

        [HttpPost("approve")]
        public async Task<Result> TestResultApprove(List<Guid> checklistItemId, CancellationToken ct) 
        {


            return Result.Ok();
        }

        /// <summary>
        /// 下载链接
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="reportNumber"></param>
        /// <returns></returns>
        [HttpGet("datasheet-{fileName}/{reportNumber}/download")]
        public IActionResult Download(string fileName,string reportNumber)
        {
            var filePath = Path.Combine(_env.WebRootPath, "DocxModel", "SaveDocx", "DataSheet", reportNumber, fileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound(new { success = false, message = "文件不存在" });

            // 根据扩展名判断 MIME 类型
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };

            return PhysicalFile(
                filePath,
                contentType,
                fileDownloadName: fileName,
                enableRangeProcessing: true
            );
        }
    }
}
