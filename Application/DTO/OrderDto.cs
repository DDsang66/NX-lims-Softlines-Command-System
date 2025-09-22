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
        public DateOnly dueDate { get; set; }
        public int? cs { get; set; }
        public string? group { get; set; }
        public DateTime labIn { get; set; }
    }


    public class OrderOutput
    {
        public string? reportNum { get; set; }
        public string? orderEntry { get; set; }
        public string? express { get; set; }
        public DateOnly dueDate { get; set; }
        public string? cs { get; set; }
        public string? testgroup { get; set; }
        public DateTime labIn { get; set; }
        public string? remark { get; set; }
        public string? status { get; set; }
    }
}
