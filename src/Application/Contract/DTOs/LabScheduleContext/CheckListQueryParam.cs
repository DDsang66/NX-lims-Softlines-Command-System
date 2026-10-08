namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.LabScheduleContext
{
    public record CheckListQueryParamDto
    {
        /// <summary>
        /// checklistId
        /// </summary>
        public string? ChecklistId { get; set; }

        /// <summary>
        /// 报告号
        /// </summary>
        public string? ReportNumber { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; } = "All";

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime? EndTime { get; set; }
        /// <summary>
        /// 页码
        /// </summary>
        public int PageNum { get; set; } = 1;

        /// <summary>
        /// 每页条数
        /// </summary>
        public int PageSize { get; set; } = 10;

    }
}
