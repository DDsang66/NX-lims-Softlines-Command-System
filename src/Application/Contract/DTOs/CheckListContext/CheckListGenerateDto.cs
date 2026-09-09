namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext
{
    public record CheckListGenerateDto
    {
        /// <summary>
        /// checklistId
        /// </summary>
        public Guid CheckListId { get; set; }

        /// <summary>
        /// 报告编号
        /// </summary>
        public string? ReportNo { get; set; }

        /// <summary>
        /// 审单人
        /// </summary>
        public string? Reviewer { get; set; }

        /// <summary>
        /// 审核时间
        /// </summary>
        public DateTimeOffset DateTime { get; set; } = DateTimeOffset.Now;

        /// <summary>
        /// 测试清单细则
        /// </summary>
        public IEnumerable<CheckListResponseItemDto> Items { get; set; } = Enumerable.Empty<CheckListResponseItemDto>();
    }
}
