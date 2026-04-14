namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs
{
    public sealed record SaveAsRequest
    {
        public string reportNum { get; set; } = string.Empty;
        public string fileUrl { get; set; } = string.Empty;
        public string fileName { get; set; } = string.Empty;
        public string group { get; set; } = string.Empty;
        public string buyer { get; set; } = string.Empty;
        public string key { get; set; } = string.Empty;
    }
}
