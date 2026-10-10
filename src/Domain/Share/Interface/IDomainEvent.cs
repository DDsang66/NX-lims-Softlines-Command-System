using MediatR;

namespace NX_lims_Softlines_Command_System.src.Domain.Share.Interface
{
    /// <summary>
    /// 领域事件接口
    /// </summary>
    public interface IDomainEvent: INotification
    {
        /// <summary>
        /// 事件的唯一标识（用于 Outbox 表的 EventId 列）
        /// </summary>
        Guid EventId { get; }

        /// <summary>
        /// 事件发生的时间（用于 Outbox 表的 OccurredOn 列）
        /// </summary>
        DateTime OccurredOn { get; }

        /// <summary>
        /// 获取聚合根ID的字符串表示（用于 Outbox 表的 AggregateRootId 列）
        /// 注意：这里返回 string 而不是泛型，因为接口不能有泛型属性
        /// </summary>
        string GetAggregateRootIdString();

        /// <summary>序列化业务数据（不含 EventId/OccurredOn/AggregateRootId 这些公共字段）</summary>
        string SerializeData();

        /// <summary>从业务数据重建事件（静态工厂，由具体事件实现）</summary>
        // 注意：静态方法不能放接口里作为实例契约，这里用另一个约定：
        // 每个事件类提供一个 public static IDomainEvent FromData(string json, ...) 方法，
        // 由 EventOutbox 通过反射调用。
    }
}
