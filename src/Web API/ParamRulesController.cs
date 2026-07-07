using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Interface;
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
        public async Task<IActionResult> CreateParamRule([FromBody] CreateParamRuleRequest request, CancellationToken ct)
        {
            var result = await _applicationService.AddParamRuleFromJsonAsync(request, ct);
            return Ok(result);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateParamRule([FromBody] UpdateParamRuleRequest request, CancellationToken ct)
        {
            var result = await _applicationService.UpdateParamRuleAsync(request, ct);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetParamRule(string id,CancellationToken ct)
        {
            var result = await _applicationService.GetParamRuleAsync(id, ct);
            return Ok(result);
        }
    }
}
