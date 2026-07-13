using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamFormulaContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParamFormulaController : ControllerBase
    {
        private readonly IFormulaAppService _formulaAppService;
        public ParamFormulaController(IFormulaAppService formulaAppService) 
        {
            _formulaAppService = formulaAppService; 
        }
        
        [HttpPost("add")]
        public async Task<IActionResult> CreateParamRuleJson([FromBody] AddFormulaDto request, CancellationToken ct)
        {
            var result = await _formulaAppService.AddFormulaAsync(request, ct);
            return Ok(result);
        }
    }
}
