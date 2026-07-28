using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TestItemContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.TestItemContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestItemController : ControllerBase
    {
        private readonly ITestItemAppService _testItemAppService;

        public TestItemController(ITestItemAppService testItemAppService) 
        {
            _testItemAppService = testItemAppService;
        }

        [HttpPut("update")]
        public async Task<Result> UpdateTestItemAsync([FromBody] UpdateTestItemDto dto, CancellationToken ct)
        {
            var result = await _testItemAppService.UpdateTestItemAsync(dto,ct);

            return result;
        }
    }
}
