using Mapster;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.BuyerContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MenuContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MenuContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class MenuRepository:IMenuRepository,IScopedDependency
    {
        private readonly dbContext _dbContext;

        public MenuRepository(dbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 添加聚合根
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <returns></returns>
        public async Task AddAsync(Menu aggregateRoot, CancellationToken ct) 
        {
             var menuPo = aggregateRoot.Adapt<BasicBuyerMenu>();

            await  _dbContext.AddAsync(menuPo, ct);

            if (aggregateRoot.MenuItems != null && aggregateRoot.MenuItems.Any())
            {
                var itemsPo = aggregateRoot.MenuItems.Adapt<List<BasicMenuItem>>();
                
                foreach (var po in itemsPo)
                {
                    po.MenuId = menuPo.MenuId;
                }
                await _dbContext.BasicMenuItems.AddRangeAsync(itemsPo, ct);
            }
        }

        /// <summary>
        /// 修改聚合根
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <returns></returns>
        public async Task UpdateAsync(Menu aggregateRoot, CancellationToken ct)
        {
            // 1. 先查询现有的Menu（不包含MenuItems，或者根据需要决定是否包含）
            var existingMenu = await _dbContext.BasicBuyerMenus
                .FirstOrDefaultAsync(m => m.MenuId == aggregateRoot.Id, ct);

            if (existingMenu == null)
                throw new InvalidOperationException($"未找到要更新的菜单: {aggregateRoot.Id.Value}");

            // 2. 将 domain 适配为 PO，并更新基本信息（避免直接传 domain 给 EF）
            var menuPo = aggregateRoot.Adapt<BasicBuyerMenu>();
            // 保持主键一致
            menuPo.MenuId = existingMenu.MenuId;
            _dbContext.Entry(existingMenu).CurrentValues.SetValues(menuPo);

            // 3. 同步更新MenuItem表
            await SyncMenuItemsAsync(aggregateRoot.Id, aggregateRoot.MenuItems, ct);
        }

        /// <summary>
        /// 同步更新菜单项（增删改）
        /// </summary>
        private async Task SyncMenuItemsAsync(MenuId menuId, IReadOnlyList<MenuItem> newItems, CancellationToken ct)
        {
            // 1. 获取数据库中现有的菜单项（根据 MenuId 查询）
            var existingItems = await _dbContext.BasicMenuItems
                .Where(item => item.MenuId == menuId.Value) // MenuItem 表中的外键 MenuId
                .ToListAsync(ct);

            // 2. 提取 MenuItem 的 ID 集合便于比较（注意：是 MenuItem 自身的 Id，不是 MenuId）
            var existingIds = existingItems.Select(x => x.Id).ToHashSet(); // ✅ 修正：取 MenuItem.Id
            var newIds = newItems?.Select(x => x.Id).ToHashSet() ?? new HashSet<Guid>();

            // 3. 计算需要删除的项（在现有中但不在新集合中）
            var idsToDelete = existingIds.Except(newIds).ToList();
            if (idsToDelete.Any())
            {
                var itemsToDelete = existingItems
                    .Where(x => idsToDelete.Contains(x.Id))
                    .ToList();
                _dbContext.BasicMenuItems.RemoveRange(itemsToDelete);
            }

            // 4. 计算需要添加的项（在新集合中但不在现有中）
            var idsToAdd = newIds.Except(existingIds).ToList();
            if (idsToAdd.Any() && newItems != null)
            {
                var itemsToAdd = newItems
                    .Where(x => idsToAdd.Contains(x.Id))
                    .ToList();

                var itemsToAddPo = itemsToAdd.Adapt<List<BasicMenuItem>>();

                // 为每个新增 PO 赋外键 MenuId
                foreach (var po in itemsToAddPo)
                {
                    po.MenuId = menuId.Value;
                }

                await _dbContext.BasicMenuItems.AddRangeAsync(itemsToAdd.Adapt<BasicMenuItem>());
            }

            // 5. 计算需要更新的项（在两者中都存在）
            var idsToUpdate = existingIds.Intersect(newIds).ToList();
            if (idsToUpdate.Any() && newItems != null)
            {
                // 创建字典以便快速查找新值
                var newItemsDict = newItems.ToDictionary(x => x.Id);

                foreach (var existingItem in existingItems)
                {
                    if (idsToUpdate.Contains(existingItem.Id) && newItemsDict.TryGetValue(existingItem.Id, out var newItem))
                    {
                        // 将 domain newItem 适配为 PO，然后用 SetValues 更新现有 PO（保留主键与外键）
                        var adaptedPo = newItem.Adapt<BasicMenuItem>();
                        adaptedPo.Id = existingItem.Id;
                        adaptedPo.MenuId = existingItem.MenuId;
                        _dbContext.Entry(existingItem).CurrentValues.SetValues(adaptedPo);
                    }
                }
            }
        }

        /// <summary>
        /// 查询聚合根
        /// </summary>
        /// <param name="aggregateRootId"></param>
        /// <param name="ct"></param>
        /// <returns>聚合根</returns>
        public async Task<Menu> GetByIdAsync(MenuId aggregateRootId, CancellationToken ct) 
        {
            // 1. 读取基本表
            var menuPo = await _dbContext.BasicBuyerMenus
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MenuId == aggregateRootId.Value, ct);

            if (menuPo == null) return null;

            // 2. 读取关联的菜单项
            var itemsPo = await _dbContext.BasicMenuItems
                .AsNoTracking()
                .Where(i => i.MenuId == aggregateRootId.Value)
                .ToListAsync(ct);

            // 3. 将 PO 转回 Domain MenuItem
            var menuItems = new List<MenuItem>();
            foreach (var po in itemsPo)
            {
                var mi = new MenuItem
                {
                    TestItemId = string.IsNullOrWhiteSpace(po.TestItemId) ? null : new TestItemId(po.TestItemId),
                    BuyerModifiedTestItemId = po.BuyerModifiedTestItem,
                    BuyerModifiedTextMethodId = po.BuyerModifiedTestMethod,
                    BuyerModifiedGroup = po.BuyerModifiedGroup,
                    Requirement = po.Requirement,
                    // StandardIds 在 DB 中以逗号分隔存储（mapping 中也使用 Join），这里拆分恢复
                    StandardIds = string.IsNullOrWhiteSpace(po.StandardId)
                        ? Enumerable.Empty<StandardId?>()
                        : po.StandardId.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => new StandardId(s.Trim()))
                            .Cast<StandardId?>()
                            .ToList()
                };

                // 恢复 Id（PO.Id -> Domain Entity.Id）
                mi.ReconstructId(po.Id);

                menuItems.Add(mi);
            }

            // 4. 重建聚合（Menu.Reconstitute）
            var menu = Menu.Reconstitute(
                new MenuId(menuPo.MenuId),
                menuItems.AsReadOnly(),
                menuPo.Remark,
                menuPo.UploadTime,
                (Status)menuPo.Status,
                new BuyerId(menuPo.BuyerCode)
            );

            return menu;
        }

        /// <summary>
        /// 获取所有聚合根
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<List<Menu>> GetAllAsync(CancellationToken ct)
        {
            var menusPo = await _dbContext.BasicBuyerMenus
                .AsNoTracking()
                .ToListAsync(ct);

            if (menusPo == null || menusPo.Count == 0)
                return new List<Menu>();

            var menuIds = menusPo.Select(m => m.MenuId).ToList();

            var itemsPo = await _dbContext.BasicMenuItems
                .AsNoTracking()
                .Where(i => menuIds.Contains(i.MenuId))
                .ToListAsync(ct);

            var itemsGrouped = itemsPo.GroupBy(i => i.MenuId).ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<Menu>(menusPo.Count);
            foreach (var menuPo in menusPo)
            {
                var poList = itemsGrouped.TryGetValue(menuPo.MenuId, out var list) ? list : new List<BasicMenuItem>();

                var menuItems = new List<MenuItem>(poList.Count);
                foreach (var po in poList)
                {
                    var mi = new MenuItem
                    {
                        TestItemId = string.IsNullOrWhiteSpace(po.TestItemId) ? null : new TestItemId(po.TestItemId),
                        BuyerModifiedTestItemId = po.BuyerModifiedTestItem,
                        BuyerModifiedTextMethodId = po.BuyerModifiedTestMethod,
                        BuyerModifiedGroup = po.BuyerModifiedGroup,
                        Requirement = po.Requirement,
                        StandardIds = string.IsNullOrWhiteSpace(po.StandardId)
                            ? Enumerable.Empty<StandardId?>()
                            : po.StandardId.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => new StandardId(s.Trim()))
                                .Cast<StandardId?>()
                                .ToList()
                    };
                    mi.ReconstructId(po.Id);
                    menuItems.Add(mi);
                }

                var menu = Menu.Reconstitute(
                    new MenuId(menuPo.MenuId),
                    menuItems.AsReadOnly(),
                    menuPo.Remark,
                    menuPo.UploadTime,
                    (Status)menuPo.Status,
                    new BuyerId(menuPo.BuyerCode)
                );

                result.Add(menu);
            }

            return result;
        }

        /// <summary>
        /// 根据买家获取菜单
        /// </summary>
        /// <param name="buyerId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<List<Menu>> GetMenusByBuyerAsync(string buyerId, CancellationToken ct) 
        {
            if (string.IsNullOrWhiteSpace(buyerId)) return new List<Menu>();

            var menusPo = await _dbContext.BasicBuyerMenus
                .AsNoTracking()
                .Where(m => m.BuyerCode == buyerId)
                .ToListAsync(ct);

            if (menusPo == null || menusPo.Count == 0)
                return new List<Menu>();

            var menuIds = menusPo.Select(m => m.MenuId).ToList();

            var itemsPo = await _dbContext.BasicMenuItems
                .AsNoTracking()
                .Where(i => menuIds.Contains(i.MenuId))
                .ToListAsync(ct);

            var itemsGrouped = itemsPo.GroupBy(i => i.MenuId).ToDictionary(g => g.Key, g => g.ToList());

            var result = new List<Menu>(menusPo.Count);
            foreach (var menuPo in menusPo)
            {
                var poList = itemsGrouped.TryGetValue(menuPo.MenuId, out var list) ? list : new List<BasicMenuItem>();

                var menuItems = new List<MenuItem>(poList.Count);
                foreach (var po in poList)
                {
                    var mi = new MenuItem
                    {
                        TestItemId = string.IsNullOrWhiteSpace(po.TestItemId) ? null : new TestItemId(po.TestItemId),
                        BuyerModifiedTestItemId = po.BuyerModifiedTestItem,
                        BuyerModifiedTextMethodId = po.BuyerModifiedTestMethod,
                        BuyerModifiedGroup = po.BuyerModifiedGroup,
                        Requirement = po.Requirement,
                        StandardIds = string.IsNullOrWhiteSpace(po.StandardId)
                            ? Enumerable.Empty<StandardId?>()
                            : po.StandardId.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                .Select(s => new StandardId(s.Trim()))
                                .Cast<StandardId?>()
                                .ToList()
                    };
                    mi.ReconstructId(po.Id);
                    menuItems.Add(mi);
                }

                var menu = Menu.Reconstitute(
                    new MenuId(menuPo.MenuId),
                    menuItems.AsReadOnly(),
                    menuPo.Remark,
                    menuPo.UploadTime,
                    (Status)menuPo.Status,
                    new BuyerId(menuPo.BuyerCode)
                );

                result.Add(menu);
            }

            return result;
        }
    }
}
