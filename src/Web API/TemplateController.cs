using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TemplateContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.TemplateContext;
using NX_lims_Softlines_Command_System.src.Application.Service.TemplateContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[controller]")]
    public class TemplateController : ControllerBase
    {
        private readonly ITemplateAppService _templateAppService;
        private readonly ITemplateQueryService _templateQueryService;

        public TemplateController(ITemplateAppService templateAppService, ITemplateQueryService templateQueryService)
        {
            _templateAppService = templateAppService;
            _templateQueryService = templateQueryService;
        }

        [HttpPost("add")]
        public async Task<Result> AddTemplate([FromForm] AddTemplateDto dto, CancellationToken ct)
        {
            var result = await _templateAppService.CreateTemplateAsync(dto, ct);

            return result;
        }

        [HttpGet("getall")]
        public async Task<Result<List<TemplateResponseDto>>> GetAllTemplateAsync(CancellationToken ct) 
        {
            var result = await _templateQueryService.GetAllTemplateAsync(ct);

            return result;
        }
    }
}
