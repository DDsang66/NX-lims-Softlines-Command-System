using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository.EventOutBoxUtil
{
    public class EventOutbox : IEventOutbox, IScopedDependency
    {
        private readonly dbContext _dbContext;

        // 统一序列化配置（P0-4 的 converter 在这里注册）
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            Converters = { new AggregateRootIdJsonConverter() }
        };

        public EventOutbox(dbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 将领域事件存入 Outbox 表（仅 Add，不 SaveChanges）。
        /// Payload 用 OutboxPayload DTO 承载，不直接序列化事件对象，避开 record/接口属性的反序列化坑。
        /// </summary>
        public async Task StoreAsync(IDomainEvent @event, CancellationToken ct)
        {
            var payload = new OutboxPayload
            {
                EventId = @event.EventId,
                OccurredOn = @event.OccurredOn,
                AggregateRootId = @event.GetAggregateRootIdString(),
                // 事件业务数据由事件自己序列化（见 IDomainEvent.SerializeData）
                Data = @event.SerializeData()
            };

            var entry = new OutboxEntry
            {
                EventId = @event.EventId,
                EventType = @event.GetType().AssemblyQualifiedName!,
                Payload = JsonSerializer.Serialize(payload, JsonOpts),
                OccurredOn = @event.OccurredOn,
                AggregateRootId = @event.GetAggregateRootIdString(),
                Published = false,
                DeadLettered = false,
                RetryCount = 0
            };

            await _dbContext.Set<OutboxEntry>().AddAsync(entry, ct);
        }

        /// <summary>
        /// 获取一批未发布且未进死信的事件。
        /// 反序列化走 OutboxPayload DTO → 事件类自身的 FromPayload 静态工厂。
        /// </summary>
        public async Task<IEnumerable<IDomainEvent>> GetUnpublishedEventsAsync(int batchSize, CancellationToken ct)
        {
            var entries = await _dbContext.Set<OutboxEntry>()
                .Where(e => !e.Published && !e.DeadLettered)
                .OrderBy(e => e.OccurredOn)
                .Take(batchSize)
                .ToListAsync(ct);

            var events = new List<IDomainEvent>();

            foreach (var entry in entries)
            {
                var eventType = ResolveType(entry.EventType)
                    ?? throw new InvalidOperationException(
                        $"无法解析事件类型：{entry.EventType}（EventId={entry.EventId}）");

                var payload = JsonSerializer.Deserialize<OutboxPayload>(entry.Payload, JsonOpts)
                    ?? throw new InvalidOperationException(
                        $"Payload 反序列化失败：{entry.EventType}（EventId={entry.EventId}）");

                // 各事件类必须提供一个静态工厂：public static IDomainEvent FromPayload(OutboxPayload p)
                var factory = eventType.GetMethod(
                    "FromPayload",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                if (factory == null)
                    throw new InvalidOperationException(
                        $"事件类型 {entry.EventType} 未实现 static FromPayload(OutboxPayload)");

                var @event = (IDomainEvent?)factory.Invoke(null, new object[] { payload })
                    ?? throw new InvalidOperationException(
                        $"FromPayload 返回 null：{entry.EventType}（EventId={entry.EventId}）");

                events.Add(@event);
            }

            return events;
        }

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

        public async Task<OutboxEntry?> GetEntryAsync(Guid eventId, CancellationToken ct)
        {
            return await _dbContext.Set<OutboxEntry>()
                .FirstOrDefaultAsync(e => e.EventId == eventId, ct);
        }

        // === 辅助：类型解析，先按限定名，失败再遍历已加载程序集 ===
        private static Type? ResolveType(string typeName)
        {
            var t = Type.GetType(typeName);
            if (t != null) return t;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = asm.GetType(typeName);
                if (t != null) return t;
            }
            return null;
        }
    }
}
