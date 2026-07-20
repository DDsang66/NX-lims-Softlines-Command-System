using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Application.Service.ParamRuleAppService;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParamRulesController : ControllerBase
    {
        private readonly IParamRuleApplicationService _applicationService;
        private readonly IParamRuleQueryService _queryService;

        public ParamRulesController(IParamRuleApplicationService applicationService, IParamRuleQueryService queryService)
        {
            _applicationService = applicationService;
            _queryService = queryService;
        }

        [HttpPost("add-json")]
        public async Task<IActionResult> CreateParamRuleJson([FromBody] CreateParamRuleRequest request, CancellationToken ct)
        {
            var result = await _applicationService.AddParamRuleFromJsonAsync(request, ct);
            return Ok(result);
        }

        [HttpPost("add-naturaltext")]
        public async Task<IActionResult> CreateParamRuleText([FromBody] NaturalLanguageRuleRequest request, CancellationToken ct)
        {
            var result = await _applicationService.AddParamRuleFromNaturalTextAsync(request, ct);
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
            var result = await _queryService.GetByIdAsync(id, ct);
            return Ok(result);
        }
    }
}
