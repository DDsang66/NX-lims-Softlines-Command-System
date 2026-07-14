using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Events
{
    /// <summary>
    /// 领域事件基类，所有领域事件继承此类
    /// </summary>
    public abstract record DomainEvent:IDomainEvent
    {
        private static readonly AsyncLocal<List<IDomainEvent>> _events = new();

        /// <summary>事件唯一标识</summary>
        public Guid EventId { get; init; } = Guid.NewGuid();

        /// <summary>事件发生时间</summary>
        public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

        /// <summary>触发事件的聚合根ID</summary>
        //public abstract Guid GetAggregateRootId();

        /// <summary>触发事件的聚合根ID</summary>
        public IAggregateRootId AggregateRootId { get; init; }

        protected DomainEvent(IAggregateRootId aggregateRootId)
        {
            AggregateRootId = aggregateRootId ?? throw new ArgumentNullException(nameof(aggregateRootId));
        }
    }
}
