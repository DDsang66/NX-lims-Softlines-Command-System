namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MenuContext
{
    public record AddMenuItemsDto
    {
        public string MenuId { get; set; } = string.Empty;
        public List<MenuItemDto> MenuItems { get; set; } = new List<MenuItemDto>();
    }
}
