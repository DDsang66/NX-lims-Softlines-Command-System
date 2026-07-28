using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.StandardContext;
using NX_lims_Softlines_Command_System.src.Application.Service.StandardContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[controller]")]
    public class StandardFamilyController : ControllerBase
    {
        private readonly IStandardFamilyAppService _standardFamilyService;
        private readonly IStandardFamilyQueryService _standardFamilyQueryService;

        public StandardFamilyController(IStandardFamilyAppService standardFamilyService,IStandardFamilyQueryService standardFamilyQueryService) 
        {
            _standardFamilyService = standardFamilyService;
            _standardFamilyQueryService = standardFamilyQueryService;
        }

        [HttpPost("add")]
        public async Task<Result> AddStandardFamily([FromBody] StandardFamilyAddDto request, CancellationToken ct)
        {
            var result = await  _standardFamilyService.AddStandardFamilyAsync(request, ct);

            return result;
        }

        [HttpPut("update")]
        public async Task<Result> UpdateStandard([FromBody] StandardFamilyUpdateDto request, CancellationToken ct)
        {
            var result = await  _standardFamilyService.UpdateStandardFamilyAsync(request, ct);

            return result;
        }

        [HttpDelete("remove/{id}")]
        public async Task<Result> RemoveStandardFamily(string id, CancellationToken ct) 
        {
            var result = await _standardFamilyService.RemoveStandardFamilyAsync(id, ct);

            return result;
        }

        [HttpGet("get/{standardFamilyId}")]
        public async Task<Result<StandaradFamilyResponseDto>> GetStandard(string standardFamilyId, CancellationToken ct)
        {
            var result = await _standardFamilyQueryService.GetStandardFamilyAsync(standardFamilyId, ct);

            return result;
        }

        [HttpGet("getall")]
        public async Task<Result<List<StandaradFamilyResponseDto>>> GetAllStandards(CancellationToken ct)
        {
            var result = await _standardFamilyQueryService.GetStandardFamiliesAsync(ct);

            return result;
        }

        [HttpPut("addstandard")]
        public async Task<Result> AddStandardToFamily([FromBody] AddStandardToFamilyDto request, CancellationToken ct) 
        {
            var result = await _standardFamilyService.AddStandardToFamilyAsync(request, ct);

            return result;
        }

        [HttpPut("addformula")]
        public async Task<Result> AddFormulaToFamily([FromBody] AddFormulaToFamilyDto request, CancellationToken ct) 
        {
            var result = await _standardFamilyService.AddFormulaToFamilyAsync(request, ct);

            return result;
        }

    }
}
