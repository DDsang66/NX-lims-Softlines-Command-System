using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.Services.ExcelService;
using NX_lims_Softlines_Command_System.Application.Services.Factory;
using NX_lims_Softlines_Command_System.Application.Services.Interfaces;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.UserService;

namespace NX_lims_Softlines_Command_System.Interfaces.Controllers
{
    [ApiController]
    [Route("api/feedback")]
    public class FeedBack : ControllerBase
    {
        private readonly FeedBackService _service;

        public FeedBack(FeedBackService service)
        {
            _service = service;
        }

        [HttpPost("post")]
        public async Task<IActionResult> Post([FromBody] FeedBackDto dto)
        {
            bool? result =await _service.Post(dto);
            if (result == true) 
            {
                return Ok(new { success = true });
            }
            return Ok(new { success = false });
        }


        [HttpGet("get")]
        public async Task<IActionResult> Get()
        {
            var result = await _service.Get();
            return Ok(new { success = true, data = result });
        }
    }
}
