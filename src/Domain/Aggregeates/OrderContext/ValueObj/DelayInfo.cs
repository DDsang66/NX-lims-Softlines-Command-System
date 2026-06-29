namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj
{
    /// <summary>
    /// 延迟信息值对象
    /// </summary>
    public sealed record DelayInfo
    {
        public string? Type { get; init; }       // "Internal" / "External"
        public string? Reason { get; init; }

        public bool HasDelay => !string.IsNullOrWhiteSpace(Type);

        public static DelayInfo None() => new();

        public static DelayInfo Create(string? type, string? reason)
            => new() { Type = type, Reason = reason };
    }
}
