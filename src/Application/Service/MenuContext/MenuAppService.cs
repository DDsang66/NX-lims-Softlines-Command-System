using DocumentFormat.OpenXml.Office2010.Excel;
using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MenuContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.MenuContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.BuyerContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MenuContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MenuContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repositories;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Application.Service.MenuContext
{
    public class MenuAppService:IScopedDependency,IMenuAppService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMenuRepository _menuRepository;

        public MenuAppService(IUnitOfWork unitOfWork, IMenuRepository menuRepository)
        {
            _unitOfWork = unitOfWork;
            _menuRepository = menuRepository;
        }

        /// <summary>
        /// 创建空套餐（草稿状态，无菜单项）
        /// </summary>
        public async Task<Result> CreateMenuAsync(AddMenuDto dto, CancellationToken ct)
        {
            // 检查是否已存在
            var exists = await _menuRepository.GetByIdAsync(new MenuId(dto.MenuId), ct);
            if (exists!=null)
                return Result.Fail($"套餐ID {dto.MenuId} 已存在");

            var menu = Menu.Create(
                new MenuId(dto.MenuId),
                dto.MenuName,
                dto.Remark,
                new BuyerId(dto.BuyerId)
            );

            await _menuRepository.AddAsync(menu, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 批量添加菜单项（支持一次性添加多个）
        /// </summary>
        public async Task<Result> AddMenuItemsAsync(AddMenuItemsDto dto, CancellationToken ct)
        {
            var menuId = new MenuId(dto.MenuId);

            // 1. 获取现有菜单
            var menu = await _menuRepository.GetByIdAsync(menuId, ct);
            if (menu == null)
                return Result.Fail($"未找到套餐: {dto.MenuId}");

            // 2. 转换DTO为领域实体
            var menuItems = dto.MenuItems.Select(item =>item.Adapt<MenuItem>()).ToList();

            // 3. 批量添加
            menu.AddMenuItems(menuItems);

            // 4. 保存
            await _menuRepository.UpdateAsync(menu, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 单个添加菜单项
        /// </summary>
        public async Task<Result> AddMenuItemAsync(AddMenuItemDto dto, CancellationToken ct)
        {
            var menuId = new MenuId(dto.MenuId);

            var menu = await _menuRepository.GetByIdAsync(menuId, ct);
            if (menu == null)
                return Result.Fail($"未找到套餐: {dto.MenuId}");

            var menuItem = dto.MenuItem.Adapt<MenuItem>();

            menu.AddMenuItem(menuItem);

            await _menuRepository.UpdateAsync(menu, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 更新菜单项
        /// </summary>
        public async Task<Result> UpdateMenuItemAsync(UpdateMenuItemDto dto, CancellationToken ct)
        {
            var menuId = new MenuId(dto.MenuId);
            var menuItemId = Guid.NewGuid();

            var menu = await _menuRepository.GetByIdAsync(menuId, ct);
            if (menu == null)
                return Result.Fail($"未找到套餐: {dto.MenuId}");

            var updatedItem = dto.MenuItem.Adapt<MenuItem>();
            menu.UpdateMenuItem(updatedItem);

            await _menuRepository.UpdateAsync(menu, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 删除套餐（软删除）
        /// </summary>
        public async Task<Result> DeleteMenuAsync(string menuId, CancellationToken ct)
        {
            var id = new MenuId(menuId);

            var menu = await _menuRepository.GetByIdAsync(id, ct);

            if (menu == null)
                return Result.Fail($"未找到套餐: {menuId}");

            /*menu.delete();*/ // 需要在 Menu 中添加 Delete 方法

            await _menuRepository.UpdateAsync(menu, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }
    }
}
