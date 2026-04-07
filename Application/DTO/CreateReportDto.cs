namespace NX_lims_Softlines_Command_System.Application.DTO
{
    public class CreateReportDto
    {
        public string ReportNum { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public string Buyer { get; set; } = "Mango";
        public string FiberContent { get; set; } = "Polyester 100%";
        // 未来可以在这里加更多字段，例如 public string Buyer { get; set; }
    }
}
