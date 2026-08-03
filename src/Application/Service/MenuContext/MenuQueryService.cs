using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MenuContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.MenuContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.BuyerContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MenuContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.MenuContext
{
    public class MenuQueryService: IScopedDependency,IMenuQueryService
    {
        private readonly IMenuRepository _menuRepository;
        private readonly IBuyerReposity _buyerRepository;

        public MenuQueryService(IMenuRepository menuRepository, IBuyerReposity buyerRepository)
        {
            _menuRepository = menuRepository;
            _buyerRepository = buyerRepository;
        }

        /// <summary>
        /// 根据套餐ID获取套餐
        /// </summary>
        /// <param name="menuId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<MenuResponseDto>> GetMenuAsync(string menuId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(menuId))
                return Result<MenuResponseDto>.Fail("menuId 不能为空");

            var menu = await _menuRepository.GetByIdAsync(new MenuId(menuId), ct);
            if (menu == null)
                return Result<MenuResponseDto>.Fail($"未找到套餐: {menuId}");

            var dto = menu.Adapt<MenuResponseDto>();
            return Result<MenuResponseDto>.Ok(dto);
        }

        /// <summary>
        /// 根据买家获取套餐
        /// </summary>
        /// <param name="buyerId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<List<MenuResponseDto>>> GetMenusByBuyerAsync(string buyerId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(buyerId))
                return Result<List<MenuResponseDto>>.Fail("buyerId 不能为空");

            var buyer = await _buyerRepository.GetByIdAsync(new BuyerId(buyerId), ct);

            if (buyer == null)
                return Result<List<MenuResponseDto>>.Fail($"未找到买家: {buyerId}");

            var menuList  = await _menuRepository.GetMenusByBuyerAsync(buyerId, ct);

            var list = menuList.Adapt<List<MenuResponseDto>>();

            return Result<List<MenuResponseDto>>.Ok(list);
        }

        /// <summary>
        /// 获取所有套餐
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<List<MenuResponseDto>>> GetMenusAsync(CancellationToken ct)
        {
            var menuList = await _menuRepository.GetAllAsync(ct);
           
            if (menuList != null && menuList.Any())
            {
                var list = menuList.Adapt<List<MenuResponseDto>>();
                return Result<List<MenuResponseDto>>.Ok(list);
            }

            return Result<List<MenuResponseDto>>.Fail("未找到套餐");
        }
    }
}
