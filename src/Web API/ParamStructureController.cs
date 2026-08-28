using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParamStructureController : ControllerBase
    {
        private readonly IParamStructureAppService _paramStructureAppService;
        private readonly IParamStructureQueryService    _paramStructureQueryService;
        public ParamStructureController(IParamStructureAppService paramStructureAppService,IParamStructureQueryService paramStructureQueryService)
        {
            _paramStructureAppService = paramStructureAppService;
            _paramStructureQueryService = paramStructureQueryService;
        }

        [HttpPost("add")]
        public async Task<Result> AddParamStructure([FromBody] AddParamStructureDto dto, CancellationToken ct)
        {
            var result = await _paramStructureAppService.AddParamStructureAsync(dto, ct);

            return result;
        }

        [HttpPut("update")]
        public async Task<Result> UpdateParamStructure([FromBody] UpdateParamStructureDto dto, CancellationToken ct)
        {
            var result = await _paramStructureAppService.UpdateParamStructureAsync(dto, ct);

            return result;
        }

        [HttpDelete("remove/{paramStructureId}")]
        public async Task<Result> RemoveParamStructure(string paramStructureId, CancellationToken ct)
        {
            var result = await _paramStructureAppService.RemoveParamStructureAsync(paramStructureId, ct);

            return result;
        }

        [HttpPut("active/{paramStructureId}")]
        public async Task<Result> ActiveParamStructure(string paramStructureId, CancellationToken ct) 
        {
            var result = await _paramStructureAppService.ActiveParamStructureAsync(paramStructureId, ct);

            return result;
        }

        [HttpGet("get/{paramStructureId}")]
        public async Task<Result<ParamStructureResponseDto>> GetParamStructure(string paramStructureId, CancellationToken ct) 
        {
            var result = await _paramStructureQueryService.GetParamStructureAsync(paramStructureId, ct);

            return result;
        }

        [HttpGet("getall")]
        public async Task<Result<List<ParamStructureResponseDto>>> GetParamStructureList(CancellationToken ct)
        {
            var result = await _paramStructureQueryService.GetAllStructureAsync(ct);

            return result;
        }

        [HttpGet("get-by-name/{paramName}")]
        public async Task<Result<List<ParamStructureResponseDto>>> GetParamStructureByName(string paramName, CancellationToken ct) 
        {
            var result = await _paramStructureQueryService.GetByParamNameAsync(paramName, ct);
         
            return result;
        }

        [HttpGet("get-by-familyId/{familyId}")]
        public async Task<Result<List<ParamStructureResponseDto>>> GetParamStructureByFamilyId(string familyId, CancellationToken ct) 
        {
            var result = await _paramStructureQueryService.GetByFamilyIdAsync(familyId, ct);

            return result;
        }
    }
}
