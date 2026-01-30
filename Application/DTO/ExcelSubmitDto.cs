namespace NX_lims_Softlines_Command_System.Application.DTO
{
    public class ExcelSubmitDto
    {
        public string? ReportNumber { get; set; }
        public string? Reviewer { get; set; }
        public string? Buyer { get; set; }
        public string? MenuName { get; set; }
        public List<SelectedRows>? SelectedRows { get; set; }
        public List<NewSelectedRows>? NewSelectedRows { get; set; }
        public string? AdditionalRequire { get; set; }
        public string? SampleDescription { get; set; }
        public List<SampleDescObject>? SampleDescripBoundSingleDto  { get; set; }
        public List<SeamDescObject>? SeamParameter { get; set; }
    }

    public class SeamDescObject
    {
        public string? Sample { get; set; }
        public List<SeamLocationObject>? LocationInfos { get; set; }
    }
    public class SeamLocationObject
    {
        public string? Location { get; set; }
        public bool? IsNA { get; set; }
        public string? Reason { get; set; }
    }


}
