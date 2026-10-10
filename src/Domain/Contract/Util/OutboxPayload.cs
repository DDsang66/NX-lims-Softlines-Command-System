namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Util
{
    /// <summary>
    /// Outbox 载荷 DTO：只存重建事件所需的最小字段。
    /// 不直接序列化 IDomainEvent，避免接口属性、record 构造器带来的反序列化问题。
    /// </summary>
    public sealed class OutboxPayload
    {
        public Guid EventId { get; set; }
        public DateTime OccurredOn { get; set; }
        public string AggregateRootId { get; set; } = string.Empty;

        /// <summary>事件业务数据（各事件自己的字段），由各事件自己序列化进来</summary>
        public string Data { get; set; } = string.Empty;
    }
}
