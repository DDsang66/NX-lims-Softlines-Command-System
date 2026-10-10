using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Domain.Share.Interface
{
    public interface IEventOutbox: IScopedDependency
    {
        /// <summary>
        /// 将事件写入 Outbox 表（仅 Add，不 SaveChanges）。
        /// 由 UnitOfWork 统一 SaveChanges，保证与业务变更同事务落库。
        /// </summary>
        Task StoreAsync(IDomainEvent @event, CancellationToken ct);

        /// <summary>
        /// 获取一批未发布且未进死信的事件（按发生时间升序）。
        /// </summary>
        Task<IEnumerable<IDomainEvent>> GetUnpublishedEventsAsync(int batchSize, CancellationToken ct);

        /// <summary>
        /// 标记事件为已发布（内部 SaveChanges）。
        /// </summary>
        Task MarkAsPublishedAsync(Guid eventId, CancellationToken ct);

        /// <summary>
        /// 递增重试次数并记录错误信息（内部 SaveChanges）。
        /// </summary>
        Task IncrementRetryAsync(Guid eventId, string error, CancellationToken ct);

        /// <summary>
        /// 标记事件为死信（内部 SaveChanges）。
        /// </summary>
        Task MarkAsDeadLetterAsync(Guid eventId, CancellationToken ct);

        /// <summary>
        /// 按 EventId 查询 Outbox 条目。
        /// </summary>
        Task<OutboxEntry?> GetEntryAsync(Guid eventId, CancellationToken ct);
    }
}
