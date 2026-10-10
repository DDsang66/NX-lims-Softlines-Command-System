using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Domain.Share
{
    /// <summary>
    /// 领域事件统一收集器（非泛型）
    /// 所有 TValue 的领域事件共用同一份 AsyncLocal 存储，避免泛型静态字段按 T 分裂。
    /// </summary>
    public static class DomainEvents
    {
        private static readonly AsyncLocal<List<IDomainEvent>> _events = new();

        public static List<IDomainEvent> Get()
        {
            return _events.Value ??= new List<IDomainEvent>();
        }

        public static void Add(IDomainEvent domainEvent)
        {
            if (domainEvent is null) throw new ArgumentNullException(nameof(domainEvent));
            Get().Add(domainEvent);
        }

        public static void Clear()
        {
            _events.Value?.Clear();
        }
    }

    /// <summary>
    /// 领域事件基类，所有领域事件继承此类
    /// </summary>
    public abstract record DomainEvent<TValue> : IDomainEvent
        where TValue : notnull
    {
        /// <summary>事件唯一标识</summary>
        public Guid EventId { get; init; } = Guid.NewGuid();

        /// <summary>事件发生时间</summary>
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

        /// <summary>触发事件的聚合根ID（强类型）</summary>
        public IAggregateRootId<TValue> AggregateRootId { get; init; }

        protected DomainEvent(IAggregateRootId<TValue> aggregateRootId)
        {
            AggregateRootId = aggregateRootId ?? throw new ArgumentNullException(nameof(aggregateRootId));
        }

        /// <summary>
        /// 把当前事件登记到统一收集器。
        /// 由聚合根在状态变更后调用，例如：new OrderCreatedEvent(Id).Register();
        /// </summary>
        public DomainEvent<TValue> Register()
        {
            DomainEvents.Add(this);
            return this;
        }

        public string GetAggregateRootIdString() => AggregateRootId.Value.ToString()!;

        /// <summary>
        /// 默认实现：子类覆写以序列化自己的业务字段。
        /// 若子类没有额外字段，可直接返回 "{}"。
        /// </summary>
        public abstract string SerializeData();

        // === 给子类用的两个辅助 ===
        protected static string SerializeJson<T>(T value)
            => JsonSerializer.Serialize(value, JsonSerializerOptions.Default);

        protected static T? DeserializeJson<T>(string json)
            => JsonSerializer.Deserialize<T>(json, JsonSerializerOptions.Default);
    }
}
