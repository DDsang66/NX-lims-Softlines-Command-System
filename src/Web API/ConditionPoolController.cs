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
        public async Task<Result> AddConditionPoolAsync(AddConditionPoolDto dto, CancellationToken ct)
        {
            var result = await  _conditionPoolAppService.AddConditionPoolAsync(dto, ct);

            return result;
        }
    }
}
