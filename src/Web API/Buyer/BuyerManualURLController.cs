using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Service.BuyerContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API.Buyer
{
    [ApiController]
    [Route("api/buyer")]
    public class BuyerManualURLController : ControllerBase
    {

        private readonly BuyerAppService _buyerAppService;

        public BuyerManualURLController(BuyerAppService buyerAppService)
        {
            _buyerAppService = buyerAppService;
        }
        /// <summary>
        /// 获取买家手册url列表
        /// </summary>
        /// <returns></returns>
        [HttpGet("manual-url")]
        public Result<List<FileInfoDto>> BuyerManual(string buyer, CancellationToken ct)
        {
            //根据buyer获取对应文件夹的所有url
            var result =  _buyerAppService.GetManualURLList(buyer,ct);

            return Result<List<FileInfoDto>>.Ok(result);
        }
    }
}
