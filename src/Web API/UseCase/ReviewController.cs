using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.UseCase;
using NX_lims_Softlines_Command_System.src.Application.UseCase;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API.UseCase
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewController : ControllerBase
    {
        private readonly ReviewUseCaseService _reviewUseCaseService;

        public ReviewController(ReviewUseCaseService reviewUseCaseService) 
        {
            _reviewUseCaseService = reviewUseCaseService;
        }

        [HttpPost("generate-checklist")]
        public async Task<Result<ConditionPoolResponseDto>> GenerateCheckList(AddCheckListDto dto, CancellationToken ct)
        {
            var result = await _reviewUseCaseService.GenerateCheckList(dto, ct);

            return result;
        }

        [HttpPost("generate-param")]
        public async Task<Result<CheckListResponseDto>> GenerateParam(List<UpdateConditionPoolDto> dto, CancellationToken ct)
        {
            var result = await _reviewUseCaseService.GenerateParam(dto, ct);

            return result;
        }
    }
}
