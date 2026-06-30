using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.Services.Factory;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.src.Application.Service.OrderAppService;


namespace NX_lims_Softlines_Command_System.Interfaces.Controllers
{
    [ApiController]
    [Route("api/order")]
    public class OrderController : Controller
    {
        private readonly OrderAppService _orderApp;
        public OrderController(OrderAppService orderAppService)
        {
            _orderApp = orderAppService;
        }
        /// <summary>
        /// 接收前端返回的Order表单
        /// </summary>
        [HttpPost("add")]
        public async Task<IActionResult> OrderAdd([FromBody] OrderDto dto)
        {
            bool answer = await _orderApp.AddOrderAsync(dto);
            if (answer)
            {
                return Ok(new { success = true, message = "Adding Succeed" });
            }
            return Ok(new { success = false, message = "Adding failed，the reportNum is already exist" });
        }

        /// <summary>
        /// 表单数据更新
        /// </summary>
        [HttpPost("update")]
        public async Task<IActionResult> OrderUpdate([FromBody] OrderUpdateDto dto)
        {
            bool result = await _orderApp.UpdateOrderAsync(dto);
            if (result)
            {
                return Ok(new { success = true, message = "Update Succeed" });
            }
            return Ok(new { success = false, message = "Update Failed，Retry" });
        }

        /// <summary>
        /// 单列数据删除
        /// </summary>
        [HttpPost("delete")]
        public async Task<IActionResult> OrderDelete([FromBody] OrderDeleteRequest odr)
        {
            bool result = await _orderApp.DeleteOrderAsync(odr);
            if (result)
            {
                return Ok(new { success = true, message = "Delete Succeed" });
            }
            return Ok(new { success = false, message = "Delete Failed" });
        }

        /// <summary>
        /// 接收前端的ueserid返回orderlist
        /// </summary>
        [HttpGet("getorder")]
        public async Task<IActionResult> GetOrder(string userId)
        {
            var result = await _orderApp.GetOrderListAsync(userId);
            return Ok(new { success = true, message = "Getting Succeed", data = result });
        }

        /// <summary>
        /// 当前月份的单量汇总,支持复杂查询
        /// </summary>
        [HttpPost("ordersummary")]
        public async Task<IActionResult> OrderSummary([FromBody] OrderQueryParams orderQueryParams)
        {
            var result = await _orderApp.GetOrderSummaryAsync(orderQueryParams);
            return Ok(new { success = true, message = "Adding Succeed", data = result });
        }




        /// <summary>
        /// 当前月份的单量报表
        /// </summary>
        [HttpGet("cards")]
        public async Task<IActionResult> OrderReporting(DateTimeOffset time,string group,string timeType)
        {
            var result = await _orderApp.GetOrderCardListAsync(time, group, timeType);
            return Ok(new { success = true, message = "Adding Succeed", data = result });
        }

        /// <summary>
        /// 当前月份的单量比例
        /// </summary>
        [HttpGet("fanChart")]
        public async Task<IActionResult> OrderRate(DateTimeOffset time, string group, string timeType)
        {
            var result = await _orderApp.GetOrderFanChartListAsync(time, group, timeType);
            return Ok(new { success = true, message = "Adding Succeed", data = result });
        }
        /// <summary>
        /// 当前月份的单量对比
        /// </summary>
        [HttpGet("lineChart")]
        public async Task<IActionResult> OrderCompare(
            [FromQuery] string group,
            [FromQuery] string timeType,
            [FromQuery] string Type,
            [FromQuery] DateTimeOffset[] time)
        {
            var result = await _orderApp.GetOrderLineChartAsync(time, group, timeType, Type);
            return Ok(new { success = true, message = "Adding Succeed", data = result });
        }

    }
}
