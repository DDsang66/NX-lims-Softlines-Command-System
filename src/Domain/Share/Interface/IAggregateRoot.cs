using NX_lims_Softlines_Command_System.src.Domain.Events;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.Domain.Share.Interface
{
    /// <summary>
    /// 标记接口：约束只有聚合根才能被 Repository 整存整取。
    /// 空接口，无技术依赖。
    /// </summary>
    public interface IAggregateRoot<TId, TValue>
        where TId : IAggregateRootId<TValue>
        where TValue : notnull
    {
        TId Id { get; } 
        IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
        void AddDomainEvent(DomainEvent<TValue> domainEvent);
        void ClearDomainEvents();
    }
}
