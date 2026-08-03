using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MenuContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.MenuContext
{
    public interface IMenuQueryService:IScopedDependency
    {
        /// <summary>
        /// 根据 MenuId 获取单个菜单
        /// </summary>
        Task<Result<MenuResponseDto>> GetMenuAsync(string menuId, CancellationToken ct);

        /// <summary>
        /// 获取指定买家的所有菜单
        /// </summary>
        Task<Result<List<MenuResponseDto>>> GetMenusByBuyerAsync(string buyerId, CancellationToken ct);

        /// <summary>
        /// 获取所有菜单（分页/筛选可另行扩展）
        /// </summary>
        Task<Result<List<MenuResponseDto>>> GetMenusAsync(CancellationToken ct);
    }
}
