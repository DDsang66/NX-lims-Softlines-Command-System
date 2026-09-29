namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.DataSheetConetxt
{
    public record RetryDatasheetDto
    {
        /// <summary>
        /// 单个重试：传 DataSheetId
        /// </summary>
        public Guid? DataSheetId { get; set; }

        /// <summary>
        /// 批量重试：传 CheckListId，重试该 checklist 下所有失败的
        /// </summary>
        public Guid? CheckListId { get; set; }
    }
}
