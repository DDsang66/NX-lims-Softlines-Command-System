using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.UserService;

namespace NX_lims_Softlines_Command_System.Interfaces.Controllers
{
    [ApiController]
    [Route("api/render")]
    public class RenderingController : ControllerBase
    {
        private readonly RenderService _service;

        public RenderingController(RenderService service)
        {
            _service = service;
        }
        /// <summary>
        /// 接收前端返回的买家名称返回选项列表
        /// </summary>
        [HttpGet("sampledesc")]
        public async Task<IActionResult> SampleDesc(string buyername)
        {
            var Results = await _service.SampleDesc(buyername);
            return Ok(new {Data = Results, success = true, message = "Loading Successed." });
        }


        /// <summary>
        /// 接收前端返回的买家名称返回选项列表
        /// </summary>
        [HttpGet("compositionsearch")]
        public async Task<IActionResult> CompostionSearch()
        {
            var Results = await _service.CompostionSearch();
            return Ok(new { Data = Results, success = true, message = "Loading Successed." });
        }
    }
}
