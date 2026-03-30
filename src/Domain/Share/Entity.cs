using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Share
{
    /// <summary>
    /// 实体基类
    /// </summary>
    public abstract class Entity
    {
        /// <summary>
        /// 统一标识
        /// </summary>
        public Guid Id { get; protected set; }

        /// <summary>
        /// 基于ID的相等性（实体核心特征）
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object? obj) =>
            obj is Entity other && Id == other.Id;

        public override int GetHashCode() => Id.GetHashCode();

        public static bool operator ==(Entity? a, Entity? b) =>
            a?.Equals(b) ?? b is null;

        public static bool operator !=(Entity? a, Entity? b) => !(a == b);

        // 3. 领域事件支持
        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected void AddDomainEvent(IDomainEvent eventItem) =>
            _domainEvents.Add(eventItem);

        public void ClearDomainEvents() => _domainEvents.Clear();

        // 4. 乐观并发（可选）
        public byte[]? RowVersion { get; protected set; }
    }
}
