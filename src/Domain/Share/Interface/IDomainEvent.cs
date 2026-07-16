namespace NX_lims_Softlines_Command_System.src.Domain.Share.Interface
{
    /// <summary>
    /// 领域事件接口
    /// </summary>
    public interface IDomainEvent 
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
    }
}
