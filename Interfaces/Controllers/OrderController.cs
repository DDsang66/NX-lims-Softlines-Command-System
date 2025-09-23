using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.Services.Factory;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.Application.Services.OrderService;
using DocumentFormat.OpenXml.Wordprocessing;


namespace NX_lims_Softlines_Command_System.Interfaces.Controllers
{
    [ApiController]
    [Route("api/order")]
    public class OrderController : Controller
    {
        private readonly OrderService _os;
        public OrderController(OrderService os)
        {
            _os = os;
        }
        /// <summary>
        /// 接收前端返回的Order表单
        /// </summary>
        [HttpPost("add")]
        public IActionResult OrderAdd([FromBody] OrderDto dto)
        {
            //调用OrderService进行处理
            //接收处理状态，status为1时表示成功
            bool answer = _os.AddOrder(dto);
            if (answer)
            {
                return Ok(new { success = true, message = "Adding Succeed" });
            }
            return Ok(new { success = false, message = "Adding Failed" });
        }

        /// <summary>
        /// 接收前端的ueserid返回orderlist
        /// </summary>
        [HttpGet("getorder")]
        public async Task<IActionResult> GetOrder([FromBody] string userId)
        {
            var result = await _os.GetOrderListAsync(userId);
            return Ok(new { success = true, message = "Adding Succeed", data = result });
        }

        /// <summary>
        /// 当前月份的单量汇总
        /// </summary>
        [HttpGet("ordersummary")]
        public async Task<IActionResult> OrderSummary(int pageNum,int pageSize,int Month)
        {
            var result = await _os.GetOrderSummaryAsync(pageNum,pageSize,Month);
            return Ok(new { success = true, message = "Adding Succeed", data = result });
        }

    }
}
