using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Interfaces;
using NX_lims_Softlines_Command_System.Application.Services.Factory;
using NX_lims_Softlines_Command_System.Models;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NX_lims_Softlines_Command_System.Interfaces.Controllers
{
    [ApiController]
    [Route("api/buyer")]
    public class BuyerController : ControllerBase
    {
        private readonly LabDbContext _db;
        private readonly IBuyerFactory _factory;
        public BuyerController(LabDbContext db, IBuyerFactory factory)
        {
            _db = db;
            _factory = factory;
        }

        /// <summary>
        /// 套餐确认接口，返回一个CheckList
        /// </summary>
        [HttpPost("confirm")]
        public async Task<IActionResult> BuyerConfirm([FromBody] RequiredInfoDto infoDto)
            => await HandleAsync(infoDto, data => _factory.CreateBuyer(data.buyer).ShowItem(data)!);

        /// <summary>
        /// 买家要求确认接口，更新一个CheckList中的param
        /// </summary>
        [HttpPost("parameter")]
        public async Task<IActionResult> ShowParameter([FromBody] RequiredInfoDto infoDto)
            => await HandleAsync(infoDto, data => _factory.CreateBuyer(data.buyer).ShowParameter(data)!);

        private async Task<IActionResult> HandleAsync(RequiredInfoDto infoDto, Func<RequiredInfoDto, Task<object>> action)
        {
            try
            {
                if (string.IsNullOrEmpty(infoDto.buyer))
                    return BadRequest(new { success = false, message = "Invalid buyer type" });

                var result = await action(infoDto);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { success = false, message = "Internal Server Error" });
            }
        }
    }
}
