using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Domain.Events
{
    /// <summary>
    /// DataSheet 状态变更为 Completed。
    /// 下游依据 TestItemId 找到对应 ChecklistItem，
    /// 检查该 item 下所有 DataSheet 是否都完成。
    /// </summary>
    public sealed record DataSheetCompletedEvent : DomainEvent<Guid>
    {
        /// <summary>
        /// 关联的 ChecklistItem 业务键。
        /// 与 ChecklistItem.TestItemId 一致，全局唯一。
        /// </summary>
        public string TestItemId { get; init; }

        /// <summary>
        /// 关联的 Checklist 业务键。
        /// </summary>
        public Guid CheckListId { get; init; }

        /// <summary>DataSheet 完成时间</summary>
        public DateTime CompletedAt { get; init; }

        /// <summary>完成时的版本号</summary>
        public int EditorVersion { get; init; }

        public DataSheetCompletedEvent(
            IAggregateRootId<Guid> dataSheetId,
            Guid checkListId,
            string testItemId,
            DateTime completedAt,
            int editorVersion)
            : base(dataSheetId)
        {
            CheckListId = checkListId;
            TestItemId = testItemId;
            CompletedAt = completedAt;
            EditorVersion = editorVersion;
        }

        public override string SerializeData()
            => JsonSerializer.Serialize(new
            {
                CheckListId,
                TestItemId,
                CompletedAt,
                EditorVersion
            });

        public static IDomainEvent FromPayload(OutboxPayload payload)
        {
            var data = JsonSerializer.Deserialize<DataShape>(payload.Data)
                ?? throw new InvalidOperationException("DataSheetCompletedEvent.Data 解析失败");

            var id = new DataSheetId(Guid.Parse(payload.AggregateRootId));

            return new DataSheetCompletedEvent(
                id,
                data.CheckListId,
                data.TestItemId,
                data.CompletedAt,
                data.EditorVersion)
            {
                EventId = payload.EventId,
                OccurredOn = payload.OccurredOn
            };
        }

        private sealed record DataShape(Guid CheckListId, string TestItemId, DateTime CompletedAt, int EditorVersion);
    }
}
