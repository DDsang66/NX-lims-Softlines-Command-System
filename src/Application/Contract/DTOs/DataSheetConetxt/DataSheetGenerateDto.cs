namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.DataSheetConetxt
{
    public record DataSheetGenerateDto
    {
        /// <summary>
        /// 测试清单Id
        /// </summary>
        public Guid CheckListId { get; set; }

        /// <summary>
        /// 报告编号
        /// </summary>
        public string ReportNumber { get; set; } = string.Empty;

        /// <summary>
        /// 是否强制重新生成
        /// </summary>
        public bool ForceRegenerate { get; set; }
    }
}
