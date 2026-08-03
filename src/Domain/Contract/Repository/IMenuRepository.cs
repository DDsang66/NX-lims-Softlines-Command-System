using NX_lims_Softlines_Command_System.Domain;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.BuyerContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MenuContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MenuContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository
{
    public interface IMenuRepository:IRepository<Menu,MenuId,string>,IScopedDependency
    {
        /// <summary>
        /// 添加聚合根
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <returns></returns>
        Task AddAsync(Menu aggregateRoot, CancellationToken ct);

        /// <summary>
        /// 修改聚合根
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <returns></returns>
        Task UpdateAsync(Menu aggregateRoot, CancellationToken ct);

        /// <summary>
        /// 查询聚合根
        /// </summary>
        /// <param name="aggregateRootId"></param>
        /// <param name="ct"></param>
        /// <returns>聚合根</returns>
        Task<Menu> GetByIdAsync(MenuId aggregateRootId, CancellationToken ct);

        /// <summary>
        /// 查询所有聚合根
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<List<Menu>> GetAllAsync(CancellationToken ct);

        /// <summary>
        /// 根据买家查询菜单
        /// </summary>
        /// <param name="buyerId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<List<Menu>> GetMenusByBuyerAsync(string buyerId, CancellationToken ct);
    }
}
