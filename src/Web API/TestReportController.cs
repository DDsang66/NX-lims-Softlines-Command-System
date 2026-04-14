using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/report")]
    public class TestReportController : ControllerBase
    {

        /// <summary>
        /// 触发验证报告的格式逻辑检查（示例接口，实际逻辑根据需求实现）
        /// </summary>
        /// <returns></returns>
        [HttpGet("report-auth")]
        public async Task<Result> ReportingAuthAsync(string repoNum,string group,string buyer)
        {
            //var result = await _reportingAppService.ReportingAuthAsync(string repoNum,string group,string buyer);

            return Result.Ok();
        }

        /// <summary>
        /// 创建报告（示例接口，实际逻辑根据需求实现）
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("report-create")]
        public async Task<Result> ReportingCreateAsync([FromBody] CreateReportDto dto)
        {
            //var result = await _reportingAppService.ReportingCreateAsync(dto);

            return Result.Ok();
        }
    }
}
