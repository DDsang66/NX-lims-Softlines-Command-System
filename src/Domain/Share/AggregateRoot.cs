using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Events;

namespace NX_lims_Softlines_Command_System.src.Domain.Share
{
    /// <summary>
    /// 聚合根基类
    /// </summary>
    public abstract class AggregateRoot : IAggregateRoot
    {
        private readonly List<DomainEvent> _domainEvents = new();

        public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected void AddDomainEvent(DomainEvent eventItem)
        {
            if (eventItem == null) throw new ArgumentNullException(nameof(eventItem));
            _domainEvents.Add(eventItem);
        }

        public void ClearDomainEvents() => _domainEvents.Clear();
    }
}
