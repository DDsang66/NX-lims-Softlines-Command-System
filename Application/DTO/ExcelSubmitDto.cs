namespace NX_lims_Softlines_Command_System.Application.DTO
{
    public class ExcelSubmitDto
    {
        public string? ReportNumber { get; set; }
        public string? Reviewer { get; set; }
        public string? Buyer { get; set; }
        public string? MenuName { get; set; }
        public List<SelectedRows>? SelectedRows { get; set; }

        public string? AdditionalRequire { get; set; }
        public string? SampleDescription { get; set; }
    }
}
