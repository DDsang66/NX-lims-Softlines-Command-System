using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Service.ParamRuleAppService;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParamRulesController : ControllerBase
    {
        private readonly IParamRuleApplicationService _applicationService;

        public ParamRulesController(IParamRuleApplicationService applicationService)
        {
            _applicationService = applicationService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> CreateParamRule([FromBody] CreateParamRuleRequest request)
        {
            var result = await _applicationService.CreateParamRuleAsync(request);
            return Ok(result);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateParamRule([FromBody] UpdateParamRuleRequest request)
        {
            var result = await _applicationService.UpdateParamRuleAsync(request);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetParamRule(string id)
        {
            var result = await _applicationService.GetParamRuleAsync(id);
            return Ok(result);
        }
    }
}
