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
        private readonly ITestItemQueryService _testItemQueryService;

        public TestItemController(ITestItemAppService testItemAppService,ITestItemQueryService  testItemQueryService) 
        {
            _testItemAppService = testItemAppService;
            _testItemQueryService = testItemQueryService;
        }

        [HttpPost("add")]
        public async Task<Result> AddTestItemAsync([FromBody] AddTestItemDto dto, CancellationToken ct)
        {
            var result = await _testItemAppService.AddTestItemAsync(dto, ct);
            
            return result;
        }

        [HttpPut("update")]
        public async Task<Result> UpdateTestItemAsync([FromBody] UpdateTestItemDto dto, CancellationToken ct)
        {
            var result = await _testItemAppService.UpdateTestItemAsync(dto,ct);

            return result;
        }

        [HttpGet("get/{testItemId}")]
        public async Task<Result<TestItemResponseDto>> GetByIdAsync(string testItemId, CancellationToken ct) 
        {
            var result = await  _testItemQueryService.GetTestItemByIdAsync(testItemId,ct);

            return result;
        }

        [HttpGet("getall")]
        public async Task<Result<List<TestItemResponseDto>>> GetAllTestItemsAsync(CancellationToken ct)
        {
            var result = await _testItemQueryService.GetTestItemsAsync(ct);

            return result;
        }
    }
}
