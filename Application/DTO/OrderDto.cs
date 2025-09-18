namespace NX_lims_Softlines_Command_System.Application.DTO
{
    public class OrderDto
    {
        public List<OrderInput>? rows { get; set; }
        public string? remark { get; set; }
        public string? id { get; set; }
    }

    public class OrderInput
    {
        public string? reportNum { get; set; }
        public string? orderEntry { get; set; }
        public string? express { get; set; }
        public DateOnly? dueDate { get; set; }
        public int? cs { get; set; }
        public string? group { get; set; }
        public DateTime? labIn { get; set; }
    }
}
