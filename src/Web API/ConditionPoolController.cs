using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConditionPoolController : ControllerBase
    {
        private readonly IConditionPoolAppService _conditionPoolAppService;
        
        public ConditionPoolController(IConditionPoolAppService conditionPoolAppService) 
        {
            _conditionPoolAppService = conditionPoolAppService;
        }

        /// <summary>
        /// 添加条件池
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPost("add")]
        public async Task<Result> AddConditionPoolAsync([FromBody] AddConditionPoolDto dto, CancellationToken ct)
        {
            var result = await  _conditionPoolAppService.AddConditionPoolAsync(dto, ct);

            return result.IsSuccess? Result.Ok() : Result.Fail(result.Error);
        }

        /// <summary>
        /// 获取条件池
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
       public async Task<Result<ConditionPoolResponseDto>> GetConditionPoolAsync(Guid id, CancellationToken ct)
        {
            var result = await _conditionPoolAppService.GetConditionPoolAsync(id, ct);

            return result;
        }

        /// <summary>
        /// 分组条件池
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPut("group")]
        public async Task<Result> GroupConditionPoolAsync([FromBody] List<UpdateConditionPoolDto> dto, CancellationToken ct) 
        {
            var result = await _conditionPoolAppService.GroupConditionPoolAsync(dto, ct);

            return result;
        }

    }
}
