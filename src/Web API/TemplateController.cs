using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TemplateContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[controller]")]
    public class TemplateController : ControllerBase
    {
        private readonly ITemplateAppService _templateAppService;

        public TemplateController(ITemplateAppService templateAppService)
        {
            _templateAppService = templateAppService;
        }

        [HttpPost("add")]
        public async Task<Result> AddTemplate([FromForm] AddTemplateDto dto, CancellationToken ct)
        {
            var result = await _templateAppService.CreateTemplateAsync(dto, ct);

            return result;
        }
    }
}
