namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MenuContext
{
    public class MenuResponseDto
    {
        /// <summary>
        /// id
        /// </summary>
        public string MenuId { get; set; }

        /// <summary>
        /// 套餐名称
        /// </summary>
        public string MenuName { get; private set; } = string.Empty;

        /// <summary>
        /// 菜单项
        /// </summary>
        public IReadOnlyList<MenuItemDto> MenuItems { get; private set; }

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
        public string Status { get; private set; }
    }
}
