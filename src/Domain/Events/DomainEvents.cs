using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Events
{
    /// <summary>
    /// 领域事件基类，所有领域事件继承此类
    /// </summary>
    public abstract record DomainEvent<TValue> : IDomainEvent
        where TValue : notnull
    {
        private static readonly AsyncLocal<List<IDomainEvent>> _events = new();

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

        // --- 以下是领域事件派发相关的静态方法 ---

        public static List<IDomainEvent> GetEvents()
        {
            return _events.Value ??= new List<IDomainEvent>();
        }

        public static void AddEvent(DomainEvent<TValue> domainEvent)
        {
            GetEvents().Add(domainEvent);
        }

        public static void ClearEvents()
        {
            _events.Value?.Clear();
        }

        // 实现接口方法
        public string GetAggregateRootIdString() => AggregateRootId.Value.ToString()!;
    }
}
