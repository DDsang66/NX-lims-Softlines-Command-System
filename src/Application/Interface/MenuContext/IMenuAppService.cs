using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MenuContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.MenuContext
{
    public interface IMenuAppService:IScopedDependency
    {
        /// <summary>
        /// 创建空套餐（草稿状态，无菜单项）
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> CreateMenuAsync(AddMenuDto dto, CancellationToken ct);

        /// <summary>
        /// 批量添加测试项目
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> AddMenuItemsAsync(AddMenuItemsDto dto, CancellationToken ct);

        /// <summary>
        /// 添加单个菜单项
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> AddMenuItemAsync(AddMenuItemDto dto, CancellationToken ct);

        /// <summary>
        /// 更新单个菜单项
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> UpdateMenuItemAsync(UpdateMenuItemDto dto, CancellationToken ct);

        /// <summary>
        /// 删除单个菜单项
        /// </summary>
        /// <param name="menuId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> DeleteMenuAsync(string menuId, CancellationToken ct);
    }
}
