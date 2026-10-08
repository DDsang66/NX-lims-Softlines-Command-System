using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext;

namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.LabScheduleContext
{
    public record LabScheduleDto
    {
        public Guid ChecklistId { get; set; } = default(Guid);
        public string ReportNumber { get; set; } = string.Empty;     // 来自 Order 或 CheckList
        public string TestGroups { get; set; } = string.Empty;      // "Physics, Wet, Fiber" 拼接
        public string Status { get; set; } = string.Empty;            // CheckList.Status（后端原始值）
        public List<CheckListItemReaderDto> Items { get; set; } = new();
    }

    // ============ 子项读模型（跨聚合拼接） ============
    public class CheckListItemReaderDto
    {
        // ---- 来自 CheckListItem 实体 ----
        public Guid ItemId { get; set; } = default(Guid);
        public string Group { get; set; } = string.Empty;             // TestGroup
        public string TestItem { get; set; } = string.Empty;            // TestItemId / BuyerModifiedTestItemId
        public string Status { get; set; } = string.Empty;          // CheckListItem.Status

        // ---- 来自 Datasheet 聚合根 ----
        public string DatasheetId { get; set; } = string.Empty;
        public string ModelKey { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Approver { get; set; } = string.Empty;

        // ---- 关联键（用于发命令） ----
        public Guid CheckListItemId { get; set; }  = default(Guid);
        public Guid CheckListId { get; set; } =  default(Guid);
    }

}
