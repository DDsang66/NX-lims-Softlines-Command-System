using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamFormulaContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParamFormulaController : ControllerBase
    {
        private readonly IFormulaAppService _formulaAppService;
        private readonly IFormulaQueryService _formulaQueryService;
        public ParamFormulaController(IFormulaAppService formulaAppService, IFormulaQueryService formulaQueryService)
        {
            _formulaQueryService = formulaQueryService;
            _formulaAppService = formulaAppService; 
        }
        
        [HttpPost("add")]
        public async Task<Result> AddFormulaAsync([FromBody] AddFormulaDto request, CancellationToken ct)
        {
            var result = await _formulaAppService.AddFormulaAsync(request, ct);
          
            return result;
        }

        [HttpPut("update")]
        public async Task<Result> UpdateFormulaAsync([FromBody] UpdateFormulaDto request, CancellationToken ct) 
        {
            var result = await _formulaAppService.UpdateFormulaAsync(request, ct);

            return result;
        }

        [HttpDelete("delete/{formulaId}")]
        public async Task<Result> DeleteFormulaAsync(string formulaId, CancellationToken ct) 
        {
            var result = await _formulaAppService.DeleteFormulaAsync(formulaId, ct);

            return result;
        }

        [HttpPut("active/{formulaId}")]
        public async Task<Result> ActiveFormulaAsync(string formulaId, CancellationToken ct) 
        {
            var result = await _formulaAppService.ActiveFormulaAsync(formulaId, ct);

            return result;
        }

        [HttpGet("get/{formulaId}")]
        public async Task<Result<FormulaResponseDto>> GetFormulaByIdAsync(string formulaId, CancellationToken ct) 
        {
            var result = await _formulaQueryService.GetFormulaByIdAsync(formulaId, ct);

            return result;
        }

        [HttpGet("get-by-ids")]
        public async Task<Result<List<FormulaResponseDto>>> GetFormulasByIdsAsync([FromQuery] IEnumerable<string> ids, CancellationToken ct)
        {
            var result = await _formulaQueryService.GetFormulasByIdsAsync(ids, ct);

            return result;
        }

        [HttpGet("getall")]
        public async Task<Result<List<FormulaResponseDto>>> GetAllFormulaAsync(CancellationToken ct)
        {
            var result = await _formulaQueryService.GetAllFormulaAsync(ct);

            return result;
        }
    }
}
