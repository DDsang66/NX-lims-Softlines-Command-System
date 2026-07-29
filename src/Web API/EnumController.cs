using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    /// <summary>
    /// 枚举字典接口：为前端提供枚举选项数据源（下拉框、筛选条件等）
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class EnumController : ControllerBase
    {
        /// <summary>
        /// 获取所有站点（Site）枚举名称列表
        /// </summary>
        /// <returns>站点名称字符串数组，如 ["NB", "XM", "BJ", "HK", "GZ"]</returns>
        [HttpGet("sites")]
        public IActionResult GetSites()
        {
            var sites = Enum.GetNames(typeof(Site));
            return Ok(sites);
        }

        /// <summary>
        /// 获取所有标准检测项目（StandardItem）枚举名称列表
        /// </summary>
        /// <returns>标准项目名称字符串数组</returns>
        [HttpGet("standard-items")]
        public IActionResult GetStandardItems()
        {
            var items = Enum.GetNames(typeof(StandardItem));
            return Ok(items);
        }

        /// <summary>
        /// 获取所有状态（Status）枚举名称列表
        /// </summary>
        /// <returns>状态名称字符串数组</returns>
        [HttpGet("statuses")]
        public IActionResult GetStatuses()
        {
            var statuses = Enum.GetNames(typeof(Status));
            return Ok(statuses);
        }
    }
}
