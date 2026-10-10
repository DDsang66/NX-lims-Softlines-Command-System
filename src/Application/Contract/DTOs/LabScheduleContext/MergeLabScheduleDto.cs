namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.LabScheduleContext
{
    public record MergeLabScheduleDto
    {
        /// <summary>
        /// 测试清单ID
        /// </summary>
        public Guid ChecklistId { get; set; }

        /// <summary>
        /// 测试单号
        /// </summary>
        public string ReportNumber { get; set; } = string.Empty;

        /// <summary>
        /// 测试组
        /// </summary>
        public string TestGroup { get; set; } = string.Empty;

        /// <summary>
        /// 选中的item
        /// </summary>
        public string[] SelectedDataSheetUrls { get; set; } = Array.Empty<string>();
    }
}
