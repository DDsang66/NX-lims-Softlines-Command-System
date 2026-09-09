using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardCompositionContext;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.UseCase;
using NX_lims_Softlines_Command_System.src.Application.Service.StandardCompositionContext;
using NX_lims_Softlines_Command_System.src.Application.UseCase;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API.UseCase
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReviewController : ControllerBase
    {
        private readonly ReviewUseCaseService _reviewUseCaseService;
        private readonly CompositionQueryService _compositionQueryService;
        private readonly IWebHostEnvironment _env;

        public ReviewController(
            ReviewUseCaseService reviewUseCaseService, 
            IWebHostEnvironment env,
            CompositionQueryService compositionQueryService) 
        {
            _env = env;
            _reviewUseCaseService = reviewUseCaseService;
            _compositionQueryService = compositionQueryService;
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

            return result.IsSuccess? result : Result<CheckListResponseDto>.Fail(result.Error);
        }

        [HttpPost("generate-completed-checklist")]
        public async Task<Result<DocxUrlResponseDto>> PrintCheckList(CheckListGenerateDto dto, CancellationToken ct) 
        {
            var result = await _reviewUseCaseService.CheckListGenerateAndPrint(dto, ct);

            return result.IsSuccess ? result : Result<DocxUrlResponseDto>.Fail(result.Error);
        }

        [HttpGet("render-composition")]
        public async Task<Result<List<CompositionResponseDto>>> RenderComposition(CancellationToken ct)
        {
            var result = await _compositionQueryService.GetFiberCompositionsAsync();

            return Result<List<CompositionResponseDto>>.Ok(result);
        }

        [HttpGet("checklist-{fileName}/download")]
        public IActionResult Download(string fileName)
        {
            var filePath = Path.Combine(_env.WebRootPath, "DocxModel", "SaveDocx", fileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound(new { success = false, message = "文件不存在" });

            return PhysicalFile(
                filePath,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileDownloadName: fileName,
                enableRangeProcessing: true
            );
        }
    }
}
