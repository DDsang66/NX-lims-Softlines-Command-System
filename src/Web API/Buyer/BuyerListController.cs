using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Service.BuyerContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API.Buyer
{
    [ApiController]
    [Route("api/buyer")]
    public class BuyerListController : ControllerBase
    {
        private readonly BuyerQueryAppService _buyerQueryAppService;

        public BuyerListController(BuyerQueryAppService buyerQueryAppService)
        {
            _buyerQueryAppService = buyerQueryAppService;
        }

        /// <summary>
        /// 获取买方列表
        /// </summary>
        /// <returns></returns>
        [HttpGet("buyer-list")]
        public async Task<Result<List<BuyerListDto>>> BuyerListAsync(CancellationToken ct)
        {
            var result = await _buyerQueryAppService.GetBuyerListAsync(ct);

            return Result<List<BuyerListDto>>.Ok(result);
        }
    }
}
