using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MenuContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.MenuContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[controller]")]
    public class MenuController : ControllerBase
    {
        private readonly IMenuAppService _appService;
        private readonly IMenuQueryService _queryService;

        public MenuController(IMenuAppService appService, IMenuQueryService queryService)
        {
            _appService = appService;
            _queryService = queryService;
        }

        [HttpGet("getall")]
        public async Task<Result<List<MenuResponseDto>>> GetMenus(CancellationToken ct)
        {
            var result = await _queryService.GetMenusAsync(ct);

            return result;
        }

        [HttpGet("get-by-id/{menuId}")]
        public async Task<Result<MenuResponseDto>> GetMenu(string menuId,CancellationToken ct)
        {
            var result = await _queryService.GetMenuAsync(menuId, ct);

            return result;
        }

        [HttpGet("get-by-buyerId/{buyerId}")]
        public async Task<Result<List<MenuResponseDto>>> GetMenusByBuyer(string buyerId, CancellationToken ct)
        {
            var result = await _queryService.GetMenusByBuyerAsync(buyerId, ct);

            return result;
        }

        [HttpPost("add")]
        public async Task<Result> CreateMenu([FromBody] AddMenuDto dto,CancellationToken ct)
        {
            var result = await _appService.CreateMenuAsync(dto, ct);

            return result;
        }

        [HttpPost("add-items")]
        public async Task<Result> AddMenuItems([FromBody] AddMenuItemsDto dto,CancellationToken ct)
        {
            var result = await _appService.AddMenuItemsAsync(dto, ct);

            return result;
        }

        [HttpPost("add/{menuId}/item")]
        public async Task<Result> AddMenuItem([FromBody] AddMenuItemDto dto,CancellationToken ct)
        {
            var result = await _appService.AddMenuItemAsync(dto, ct);

            return result;
        }

        [HttpPut("update/{menuId}/item")]
        public async Task<Result> UpdateMenuItem([FromBody] UpdateMenuItemDto dto,CancellationToken ct)
        {
            var result = await _appService.UpdateMenuItemAsync(dto, ct);

            return result;
        }

        [HttpDelete("delete/{menuId}")]
        public async Task<Result> DeleteMenu(string menuId,CancellationToken ct)
        {
            var result = await _appService.DeleteMenuAsync(menuId, ct);

            return result;
        }
    }
}
