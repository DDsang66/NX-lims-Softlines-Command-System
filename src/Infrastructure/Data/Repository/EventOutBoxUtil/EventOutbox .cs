using Microsoft.EntityFrameworkCore;
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
        /// </summary>
        /// <param name="event">非泛型接口，可接受任何 DomainEvent<TValue></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task StoreAsync(IDomainEvent @event, CancellationToken ct) // 改为 IDomainEvent
        {
            // 注意这里提取 AggregateRootId 的方式：
            // 因为 IDomainEvent 没有泛型的 AggregateRootId 属性，
            // 我们需要通过反射或者动态获取，最简单的方式是利用 object 的 ToString()
            // 你之前在 AggregateRootId 基类中重写了 ToString() 返回 Value.ToString()，所以这里直接用即可！

            var entry = new OutboxEntry
            {
                EventId = @event.EventId,
                EventType = @event.GetType().FullName!, // 存储的是具体泛型类的完整名称，如 "xxx.TestItemCreatedEvent"
                Payload = JsonSerializer.Serialize(@event, @event.GetType()),
                OccurredOn = @event.OccurredOn,
                // 动态获取 AggregateRootId 的字符串值
                AggregateRootId = GetAggregateRootIdString(@event),
                Published = false
            };

            await _dbContext.Set<OutboxEntry>().AddAsync(entry, ct);
        }

        /// <summary>
        /// 获取一批未发布的事件
        /// </summary>
        /// <param name="batchSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IEnumerable<IDomainEvent>> GetUnpublishedEventsAsync(int batchSize, CancellationToken ct) // 返回 IDomainEvent
        {
            var entries = await _dbContext.Set<OutboxEntry>()
                .Where(e => !e.Published)
                .OrderBy(e => e.OccurredOn)
                .Take(batchSize)
                .ToListAsync(ct);

            var events = new List<IDomainEvent>(); // 存储为 IDomainEvent

            foreach (var entry in entries)
            {
                var eventType = Type.GetType(entry.EventType);
                if (eventType != null)
                {
                    // 反序列化为具体的泛型类（如 DomainEvent<string> 的子类）
                    var @event = JsonSerializer.Deserialize(entry.Payload, eventType) as IDomainEvent;
                    if (@event != null) events.Add(@event);
                }
            }

            return events;
        }

        /// <summary>
        /// 标记一个事件为已发布
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task MarkAsPublishedAsync(Guid eventId, CancellationToken ct)
        {
            var entry = await _dbContext.Set<OutboxEntry>()
                .FirstOrDefaultAsync(e => e.EventId == eventId, ct);

            if (entry != null)
            {
                entry.Published = true;
                entry.PublishedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(ct);
            }
        }

        /// <summary>
        /// 增加重试次数
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
        /// 标记一个事件为死信
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
        /// 获取一个事件
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<OutboxEntry?> GetEntryAsync(Guid eventId, CancellationToken ct)
        {
            return await _dbContext.Set<OutboxEntry>()
                .FirstOrDefaultAsync(e => e.EventId == eventId, ct);
        }

        // === 辅助方法：提取聚合根 ID 字符串 ===
        private string GetAggregateRootIdString(IDomainEvent @event)
        {
            // 因为具体的 DomainEvent<TValue> 继承了包含 AggregateRootId 属性的基类，
            // 我们可以通过反射获取这个属性的值，并调用其 ToString()。
            // 由于我们在 AggregateRootId 基类重写了 ToString() 返回 Value.ToString()，
            // 这是最通用且安全的做法。

            var property = @event.GetType().GetProperty("AggregateRootId");
            if (property != null)
            {
                var idValue = property.GetValue(@event);
                return idValue?.ToString() ?? string.Empty;
            }

            return string.Empty;
        }
    }
}
