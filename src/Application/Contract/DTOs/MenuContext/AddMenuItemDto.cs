namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MenuContext
{
    public record AddMenuItemDto
    {
        public string MenuId { get; set; } = string.Empty;
        public MenuItemDto MenuItem { get; set; } = new MenuItemDto();
    }
}
