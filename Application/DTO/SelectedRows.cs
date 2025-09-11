namespace NX_lims_Softlines_Command_System.Application.DTO
{
    public class SelectedRows
    {
        public string? itemName { get; set; }
        public string? standards { get; set; }
        public string? parameters { get; set; }
        public string? types { get; set; }
        public string? samples { get; set; }
        public bool? selected { get; set; }

        public object? extra { get; set; }
    }
}
