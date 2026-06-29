namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj
{
    /// <summary>
    /// 订单元数据值对象（录入人、客服、备注、最后更新时间）
    /// </summary>
    public sealed record OrderMetadata
    {
        public string OrderEntryPerson { get; init; } = string.Empty;
        public string CustomerService { get; init; } = string.Empty;
        public string? Remark { get; init; }
        public DateTimeOffset LastUpdateTime { get; init; }

        public static OrderMetadata Create(
            string orderEntryPerson,
            string customerService,
            string? remark,
            DateTimeOffset now)
            => new()
            {
                OrderEntryPerson = orderEntryPerson,
                CustomerService = customerService,
                Remark = remark,
                LastUpdateTime = now
            };
    }
}
