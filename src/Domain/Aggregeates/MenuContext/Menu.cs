using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.BuyerContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MenuContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MenuContext
{
    public sealed class Menu: AggregateRoot<MenuId,string>
    {
        /// <summary>
        /// 套餐名称
        /// </summary>
        public string MenuName { get; private set; } = string.Empty;

        /// <summary>
        /// 菜单项
        /// </summary>
        public IReadOnlyList<MenuItem> MenuItems { get; private set; } = Array.Empty<MenuItem>();

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; private set; } = string.Empty;

        /// <summary>
        /// 上传时间
        /// </summary>
        public DateTime UpLoadTime { get; private set; }

        /// <summary>
        /// 状态
        /// </summary>
        public Status Status { get; private set; }

        /// <summary>
        /// 关联买家
        /// </summary>
        public BuyerId BuyerId { get; private set; }

        // 内部可变集合（用于领域操作）
        private List<MenuItem> _menuItems = new();

        /// <summary>
        /// 创建空菜单（仅包含基本信息，无菜单项）
        /// </summary>
        public static Menu Create(
            MenuId id,
            string menuName,
            string? remark,
            BuyerId buyerId)
        {
            if (id == null) 
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(menuName))
                throw new ArgumentException("套餐名称不能为空", nameof(menuName));
            if (buyerId == null) throw new ArgumentNullException(nameof(buyerId));

            return new Menu
            {
                Id = id,
                MenuName = menuName.Trim(),
                _menuItems = new List<MenuItem>(),
                MenuItems = Array.Empty<MenuItem>(),
                Remark = remark,
                UpLoadTime = DateTime.UtcNow,
                Status = Status.Draft,
                BuyerId = buyerId
            };
        }

        /// <summary>
        /// 重建
        /// </summary>
        /// <returns></returns>
        public static Menu Reconstitute(
            MenuId id,
            string menuName,
            IReadOnlyList<MenuItem> menuItems,
            string? remark,
            DateTime upLoadTime,
            Status status,
            BuyerId buyerId)
        {
            var menu = new Menu
            {
                Id = id,
                MenuName = menuName,
                MenuItems = menuItems,
                Remark = remark,
                UpLoadTime = upLoadTime,
                Status = status,
                BuyerId = buyerId
            };
            // 同步内部可变列表，保证后续 AddMenuItem/RemoveMenuItem/UpdateMenuItem 基于完整 items 操作
            menu._menuItems = menuItems?.ToList() ?? new List<MenuItem>();
            return menu;
        }


        // ============ 菜单项管理 ============

        /// <summary>
        /// 添加菜单项
        /// </summary>
        public void AddMenuItem(MenuItem item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (Status == Status.Deprecated)
                throw new InvalidOperationException($"套餐已{Status}，不能添加测试项目");

            // 检查是否已存在相同项（TestItemId 非空才比较；Standard 项目的 TestItemId 为 null，不参与去重）
            if (item.TestItemId != null &&
                _menuItems.Any(x => x.TestItemId == item.TestItemId))
                throw new InvalidOperationException("该项目已存在于套餐中");

            _menuItems.Add(item);
            SyncReadOnlyList();
        }

        /// <summary>
        /// 批量添加菜单项
        /// </summary>
        public void AddMenuItems(IEnumerable<MenuItem> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (Status == Status.Deprecated)
                throw new InvalidOperationException($"套餐已{Status}，不能添加测试项目");

            var itemList = items.ToList();
            if (itemList.Any(x => x == null))
                throw new ArgumentException("套餐不能包含空值", nameof(items));

            // 检查重复（只对非空 TestItemId 去重；Standard 项目 TestItemId 为 null 不参与）
            var existingDishIds = _menuItems.Select(x => x.TestItemId).Where(id => id != null).ToHashSet();
            var duplicates = itemList.Where(x => x.TestItemId != null && existingDishIds.Contains(x.TestItemId)).ToList();
            if (duplicates.Any())
                throw new InvalidOperationException($"以下项目已存在于套餐中: {string.Join(", ", duplicates.Select(x => x.TestItemId))}");

            _menuItems.AddRange(itemList);
            SyncReadOnlyList();
        }

        /// <summary>
        /// 测试项目
        /// </summary>
        public void RemoveMenuItem(Guid itemId)
        {
            if (itemId == null) throw new ArgumentNullException(nameof(itemId));
            if (Status == Status.Deprecated)
                throw new InvalidOperationException($"套餐已{Status}，不能添加测试项目");
            if (_menuItems.Count == 0)
                throw new InvalidOperationException("菜单中暂无菜单项可移除");

            var removed = _menuItems.RemoveAll(x => x.Id == itemId);
            if (removed == 0)
                throw new InvalidOperationException($"未找到要移除的菜单项: {itemId}");

            SyncReadOnlyList();
        }

        /// <summary>
        /// 更新菜单项（替换原有项）
        /// </summary>
        public void UpdateMenuItem(MenuItem updatedItem)
        {
            if (updatedItem == null) throw new ArgumentNullException(nameof(updatedItem));
            if (Status == Status.Deprecated)
                throw new InvalidOperationException($"套餐已{Status}，不能添加测试项目");

            var index = _menuItems.FindIndex(x => x.Id == updatedItem.Id);
            if (index == -1)
                throw new InvalidOperationException($"未找到要更新的菜单项: {updatedItem.Id}");

            _menuItems[index] = updatedItem;
            SyncReadOnlyList();
        }

        /// <summary>
        /// 清空所有菜单项
        /// </summary>
        public void ClearMenuItems()
        {
            if (Status == Status.Deprecated)
                throw new InvalidOperationException($"套餐已{Status}，不能添加测试项目");

            _menuItems.Clear();
            SyncReadOnlyList();
        }

        // ============ 状态流转 ============


        // ============ 同步方法 ============

        private void SyncReadOnlyList()
        {
            MenuItems = _menuItems.ToList().AsReadOnly();
        }
    }
}
