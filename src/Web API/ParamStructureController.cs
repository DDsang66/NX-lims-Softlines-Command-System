using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[controller]")]
    public class ParamStructureController : ControllerBase
    {
        private readonly IParamStructureAppService _paramStructureAppService;
        public ParamStructureController(IParamStructureAppService paramStructureAppService)
        {
            _paramStructureAppService = paramStructureAppService;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddParamStructure([FromBody] AddParamStructureDto dto, CancellationToken ct)
        {
            var result = await _paramStructureAppService.AddParamStructureAsync(dto, ct);

            return Ok(result);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateParamStructure([FromBody] UpdateParamStructureDto dto, CancellationToken ct)
        {
            var result = await _paramStructureAppService.UpdateParamStructureAsync(dto, ct);

            return Ok(result);
        }

        [HttpDelete("remove/{paramStructureId}")]
        public async Task<IActionResult> RemoveParamStructure(string paramStructureId, CancellationToken ct)
        {
            var result = await _paramStructureAppService.RemoveParamStructureAsync(paramStructureId, ct);

            return Ok(result);
        }
    }
}
