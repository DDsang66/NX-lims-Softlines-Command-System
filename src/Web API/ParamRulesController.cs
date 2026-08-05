using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Share;

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
        public async Task<Result> CreateParamRuleJson([FromBody] CreateParamRuleRequest request, CancellationToken ct)
        {
            var result = await _applicationService.AddParamRuleFromJsonAsync(request, ct);

            return result;
        }

        [HttpPost("add-naturaltext")]
        public async Task<Result> CreateParamRuleText([FromBody] NaturalLanguageRuleRequest request, CancellationToken ct)
        {
            var result = await _applicationService.AddParamRuleFromNaturalTextAsync(request, ct);

            return result;
        }

        [HttpPut("update-json")]
        public async Task<Result> UpdateParamRuleWithJson([FromBody] UpdateParamRuleJsonRequest request, CancellationToken ct)
        {
            var result = await _applicationService.UpdateParamRuleWithJsonAsync(request, ct);

            return result;
        }

        [HttpPut("update-naturaltext")]
        public async Task<Result> UpdateParamRuleWithText([FromBody] UpdateParamRuleTextRequest request, CancellationToken ct)
        {
            var result = await _applicationService.UpdateParamRuleWithNaturalTextAsync(request, ct);

            return result;
        }

        /// <summary>
        /// 激活规则
        /// </summary>
        [HttpPut("active/{ruleId}")]
        public async Task<Result> ActivateParamRule(string ruleId, CancellationToken ct)
        {
            var result = await _applicationService.ActiveParamRuleAsync(ruleId, ct);
            return result;
        }

        /// <summary>
        /// 禁用规则
        /// </summary>
        [HttpPut("deactive/{ruleId}")]
        public async Task<Result> DeactiveParamRule(string ruleId, CancellationToken ct)
        {
            var result = await _applicationService.DeactiveParamRuleAsync(ruleId, ct);
            return result;
        }

        [HttpGet("get/{id}")]
        public async Task<Result<ParamRuleResponseDto>> GetParamRule(string id, CancellationToken ct)
        {
            var result = await _queryService.GetByIdAsync(id, ct);

            return result;
        }

        [HttpGet("get-by-ids")]
        public async Task<Result<List<ParamRuleResponseDto>>> GetParamRuleByIds([FromQuery] IEnumerable<string> ids, CancellationToken ct)
        {
            var result = await _queryService.GetByIdsAsync(ids, ct);

            return result;
        }

        [HttpGet("getall")]
        public async Task<Result<List<ParamRuleResponseDto>>> GetAllParamRules(CancellationToken ct)
        {
            var result = await _queryService.GetAllRulesAsync(ct);

            return result;
        }

        [HttpGet("getfrom-formulaId/{formulaId}")]
        public async Task<Result<List<ParamRuleResponseDto>>> GetAllParamRulesWithFormulaId(string formulaId, CancellationToken ct)
        {
            var result = await _queryService.GetRulesByFormulaIdAsync(formulaId, ct);

            return result;
        }
    }
}
