namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj
{
    /// <summary>
    /// 订单标识值对象（即 ReportNumber，如 "87.405.26.0001.01"）
    /// </summary>
    public sealed record OrderId
    {
        public string Value { get; }

        public OrderId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("ReportNumber is required", nameof(value));
            Value = value;
        }

        public override string ToString() => Value;

        public bool Equals(string? other) => Value == other;

        public static implicit operator string(OrderId id) => id.Value;
        public static implicit operator OrderId(string value) => new(value);
    }
}
