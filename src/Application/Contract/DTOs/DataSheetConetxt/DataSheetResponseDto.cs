namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.DataSheetConetxt
{
    public record DataSheetResponseDto
    {
        public string Id { get; set; } = string.Empty;

        public string BacthId { get; set; } = string.Empty;

        public string ReportNumber { get; set; } = string.Empty;

        /// <summary>
        /// 工作清单id
        /// </summary>
        public Guid CheckListId { get; set; }

        /// <summary>
        /// 测试项目id
        /// </summary>
        public string TestItemId { get; set; } = string.Empty;

        /// <summary>
        /// url
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// model 唯一标识
        /// </summary>
        public string ModelKey { get; set; } = string.Empty;

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdateTime { get; set; }
    }
}
