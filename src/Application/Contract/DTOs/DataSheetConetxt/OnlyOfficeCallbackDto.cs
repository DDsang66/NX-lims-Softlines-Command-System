namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.DataSheetConetxt
{
    public class OnlyOfficeCallbackDto
    {
        public int Status { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public List<string> Users { get; set; } = new();
    }
}
