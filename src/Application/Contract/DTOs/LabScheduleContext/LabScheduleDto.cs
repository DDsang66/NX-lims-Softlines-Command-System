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

        // ---- 来自 Datasheet 聚合根：一个 item 可能对应多份（多 model 分包）----

        /// <summary>
        /// 该 CheckListItem 下的全部 DataSheet 分包，按 ModelIndex 升序排列。
        /// 尚未生成任何 DataSheet 时为空集合（不会是 null）。
        /// </summary>
        public List<DataSheetPartDto> DataSheets { get; set; } = new();

        /// <summary>
        /// 主 DataSheet 的 Id（ModelIndex 最小的一份），等价于 <c>DataSheets[0].DataSheetId</c>。
        /// 保留该单值字段是为了兼容只读取一份报告的前端调用方。
        /// </summary>
        public string DatasheetId => DataSheets.FirstOrDefault()?.DataSheetId.ToString() ?? string.Empty;

        /// <summary>
        /// 主 DataSheet 的模型标识，等价于 <c>DataSheets[0].ModelKey</c>。
        /// </summary>
        public string ModelKey => DataSheets.FirstOrDefault()?.ModelKey ?? string.Empty;

        /// <summary>
        /// 主 DataSheet 的文件访问地址，等价于 <c>DataSheets[0].Url</c>。
        /// </summary>
        public string Url => DataSheets.FirstOrDefault()?.Url ?? string.Empty;

        public string Approver { get; set; } = string.Empty;

        // ---- 关联键（用于发命令） ----
        public Guid CheckListItemId { get; set; }  = default(Guid);
        public Guid CheckListId { get; set; } =  default(Guid);
    }

    /// <summary>
    /// 单个 DataSheet 分包（多 model 场景下每个 model 一份）的展示信息。
    /// </summary>
    public class DataSheetPartDto
    {
        /// <summary>DataSheet 聚合根的 Id。</summary>
        public Guid DataSheetId { get; set; } = default(Guid);

        /// <summary>分包序号：0 为主包，依次递增。</summary>
        public int ModelIndex { get; set; }

        /// <summary>模型标识，取决于生成时落库的 ModelKey。</summary>
        public string ModelKey { get; set; } = string.Empty;

        /// <summary>生成后的 docx 访问地址；尚未生成时为字符串空。</summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>该 DataSheet 自身的状态（Pending / Generating / Success / Failed 等）。</summary>
        public string Status { get; set; } = string.Empty;
    }

}
