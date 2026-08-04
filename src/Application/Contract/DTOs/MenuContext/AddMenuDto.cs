using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MenuContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;

namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MenuContext
{
    public record AddMenuDto
    {
        /// <summary>
        /// id
        /// </summary>
        public string MenuId { get; set; } = string.Empty;

        /// <summary>
        /// 套餐名称
        /// </summary>
        public string MenuName { get; set; } = string.Empty;

        /// <summary>
        /// 菜单项
        /// </summary>
        public IReadOnlyList<MenuItemDto> MenuItems { get;  set; } = Array.Empty<MenuItemDto>();

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get;  set; } = string.Empty;

        /// <summary>
        /// 上传时间
        /// </summary>
        public DateTime UpLoadTime { get;  set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get;  set; } = string.Empty;

        /// <summary>
        /// 买家Id
        /// </summary>
        public string BuyerId { get; set; } = string.Empty;
    }

    public record MenuItemDto
    {
        /// <summary>
        /// 菜单项ID（Guid；新建可不传，更新/删除时必传）
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// 测试项目ID
        /// </summary>
        public string? TestItemId { get; set; }

        /// <summary>
        /// 买家自定义名称
        /// </summary>
        public string? BuyerOwnName { get; set; }

        /// <summary>
        /// 买家自定义测试项目ID
        /// </summary>
        public string? BuyerModifiedTestItemId { get; set; } = string.Empty;

        /// <summary>
        /// 标准ID
        /// </summary>
        public IEnumerable<string?> StandardIds { get; set; } = Enumerable.Empty<string>();

        /// <summary>
        /// 买家自定义测试方法ID
        /// </summary>
        public string? BuyerModifiedTextMethodId { get; set; } = string.Empty;

        /// <summary>
        /// 限值
        /// </summary>
        public string? BuyerModifiedGroup { get; set; } = string.Empty;

        /// <summary>
        /// 限值
        /// </summary>
        public string? Requirement { get; set; } = string.Empty;
    }
}
