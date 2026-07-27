using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamFormulaContext;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.StandardContext;
using NX_lims_Softlines_Command_System.src.Application.Service.StandardContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[controller]")]
    public class StandardController : ControllerBase
    {
        private readonly IStandardAppService _standardAppService;
        private readonly IStandardQueryService _standardQueryService;

        public StandardController(
            IStandardAppService standardAppService,
            IStandardQueryService standardQueryService)
        {
            _standardAppService = standardAppService;
            _standardQueryService = standardQueryService;
        }

        [HttpPost("add")]
        public async Task<Result> AddStandard([FromBody] StandardAddDto request, CancellationToken ct)
        {
            var result = await _standardAppService.AddStandardAsync(request, ct);

            return result;
        }

        [HttpPut("update")]
        public async Task<Result> UpdateStandard([FromBody] StandardUpdateDto request, CancellationToken ct) 
        {
            var result = await _standardAppService.UpdateStandardAsync(request, ct);

            return result;
        }

        [HttpDelete("remove/{standardId}")]
        public async Task<Result> RemoveStandard(string standardId, CancellationToken ct)
        {
            var result = await _standardAppService.RemoveStandardAsync(standardId, ct);

            return result;
        }

        [HttpGet("get/{standardId}")]
        public async Task<Result<StandardResponseDto>> GetStandard(string standardId, CancellationToken ct) 
        {
            var result = await _standardQueryService.GetStandardAsync(standardId, ct);

            return result;
        }

        [HttpGet("getall")]
        public async Task<Result<List<StandardResponseDto>>> GetAllStandards(CancellationToken ct) 
        {
            var result = await _standardQueryService.GetStandardsAsync(ct);

            return result;
        }

        [HttpPut("active/{standardId}")]
        public async Task<Result> ActiveStandard(string standardId, CancellationToken ct) 
        {
            var result = await _standardAppService.ActiveStandardAsync(standardId, ct);

            return result;
        }

        [HttpPut("deprecate/{standardId}")]
        public async Task<Result> DeprecateStandard(string standardId, CancellationToken ct)
        {
            var result = await _standardAppService.DeprecatedStandardAsync(standardId, ct);

            return result;
        }

        [HttpPost("draft/{standardId}")]
        public async Task<Result> DraftStandard(string standardId, CancellationToken ct) 
        {
            var result = await _standardAppService.DraftStandardAsync(standardId, ct);

            return result;
        }
    }
}
