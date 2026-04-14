namespace NX_lims_Softlines_Command_System.src.Domain.Share
{
    public sealed record IdStandard
    {
        public string Value { get; }

        public IdStandard(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));

            if (value.Length > 50)
                throw new ArgumentException("IdStandard cannot exceed 50 characters.", nameof(value));
            this.Value = value;
        }

        // 2. 显式重写 ToString，解决输出 {IdStandard { Value = ... }} 的问题
        public override string ToString() => Value;

        // 3. 实现与字符串的比较逻辑
        public bool Equals(string? other) => Value == other;

        // 4. 保持原有的隐式转换
        public static implicit operator string(IdStandard code) => code.Value;
        public static implicit operator IdStandard(string value) => new(value);
    }

}
