using Microsoft.AspNetCore.Mvc;
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

        public LabScheduleController(LabScheduleUseCaseService useCase) 
        {
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
    }
}
