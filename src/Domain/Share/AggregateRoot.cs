using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Events;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Share
{
    /// <summary>
    /// 聚合根基类
    /// </summary>
    public abstract class AggregateRoot<TId,TValue> : IAggregateRoot<TId,TValue>
        where TId : IAggregateRootId<TValue>
        where TValue : notnull
    {
        private readonly List<IDomainEvent> _domainEvents = new();

        // 暴露非泛型集合
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        public TId Id { get; protected set; } = default!;

        /// <summary>
        /// 空构造函数，防止外部直接实例化
        /// </summary>
        protected AggregateRoot() { }

        protected AggregateRoot(TId id)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

        /// <summary>
        /// 添加领域事件
        /// </summary>
        /// <param name="eventItem"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddDomainEvent(DomainEvent<TValue> eventItem)
        {
            if (eventItem == null) throw new ArgumentNullException(nameof(eventItem));
            // 内部将强类型事件存入非泛型列表
            _domainEvents.Add(eventItem);
        }

        /// <summary>
        /// 清空领域事件
        /// </summary>
        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}
