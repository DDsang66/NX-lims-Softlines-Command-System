using Microsoft.AspNetCore.Mvc;
<<<<<<< HEAD
=======
using NX_lims_Softlines_Command_System.Application.DTO;
>>>>>>> a165edfa3a67107084b86ae3ea4e30814c7f9bc4
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
<<<<<<< HEAD
=======

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
>>>>>>> a165edfa3a67107084b86ae3ea4e30814c7f9bc4
    }
}
