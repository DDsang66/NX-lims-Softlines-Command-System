using NX_lims_Softlines_Command_System.src.Domain.Events;

namespace NX_lims_Softlines_Command_System.src.Domain.Share.Interface
{
    /// <summary>
    /// 空接口，用于标识聚合根的ID
    /// </summary>
    public interface IAggregateRootId<TId> : IEquatable<IAggregateRootId<TId>>
        where TId : notnull
    {
        TId Value { get; }
    }
}
