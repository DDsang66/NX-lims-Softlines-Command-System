using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Events;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository.EventOutBoxUtil
{
    public class EventOutbox : IEventOutbox,IScopedDependency
    {
        private readonly dbContext _dbContext;

        public EventOutbox(dbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 将一个领域事件存入 Outbox 表
        /// 这个方法通常在业务事务中被调用，与修改聚合根的操作在同一个 DbContext 和事务中
        /// </summary>
        /// <param name="event"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task StoreAsync(DomainEvent @event, CancellationToken ct)
        {
            var entry = new OutboxEntry
            {
                EventId = @event.EventId,
                EventType = @event.GetType().FullName!,
                Payload = JsonSerializer.Serialize(@event, @event.GetType()),
                OccurredOn = @event.OccurredOn,
                AggregateRootId = @event.AggregateRootId.ToString()!,
                Published = false
            };

            await _dbContext.Set<OutboxEntry>().AddAsync(entry, ct);
        }

        /// <summary>
        /// 获取一批未发布的事件。这是后台消费者调用的核心方法
        /// </summary>
        /// <param name="batchSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IEnumerable<DomainEvent>> GetUnpublishedEventsAsync(int batchSize, CancellationToken ct)
        {
            var entries = await _dbContext.Set<OutboxEntry>()
                .Where(e => !e.Published)
                .OrderBy(e => e.OccurredOn)
                .Take(batchSize)
                .ToListAsync(ct);

            var events = new List<DomainEvent>();
            foreach (var entry in entries)
            {
                var eventType = Type.GetType(entry.EventType);
                if (eventType != null)
                {
                    var @event = JsonSerializer.Deserialize(entry.Payload, eventType) as DomainEvent;
                    if (@event != null) events.Add(@event);
                }
            }

            return events;
        }

        /// <summary>
        /// 在成功处理一个事件后，将其标记为已发布
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task MarkAsPublishedAsync(Guid eventId, CancellationToken ct)
        {
            var entry = await  _dbContext.Set<OutboxEntry>()
                .FirstOrDefaultAsync(e => e.EventId == eventId, ct);

            if (entry != null)
            {
                entry.Published = true;
                entry.PublishedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(ct);
            }
        }

        /// <summary>
        /// 事件处理失败时，增加重试计数并记录错误
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="error"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task IncrementRetryAsync(Guid eventId, string error, CancellationToken ct)
        {
            var entry = await _dbContext.Set<OutboxEntry>()
                .FirstOrDefaultAsync(e => e.EventId == eventId, ct);

            if (entry != null)
            {
                entry.RetryCount++;
                entry.Error = error;
                await _dbContext.SaveChangesAsync(ct);
            }
        }

        /// <summary>
        /// 将无法处理的事件标记为“死信”（Dead Letter）
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task MarkAsDeadLetterAsync(Guid eventId, CancellationToken ct)
        {
            var entry = await _dbContext.Set<OutboxEntry>()
                .FirstOrDefaultAsync(e => e.EventId == eventId, ct);

            if (entry != null)
            {
                entry.DeadLettered = true;
                await _dbContext.SaveChangesAsync(ct);
            }
        }

        /// <summary>
        /// 根据事件ID查询出箱记录，通常用于调试或特定场景下的查询
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<OutboxEntry?> GetEntryAsync(Guid eventId, CancellationToken ct)
        {
            return await _dbContext.Set<OutboxEntry>()
                .FirstOrDefaultAsync(e => e.EventId == eventId, ct);
        }
    }
}
