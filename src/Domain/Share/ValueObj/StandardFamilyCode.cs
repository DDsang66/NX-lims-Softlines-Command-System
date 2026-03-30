namespace NX_lims_Softlines_Command_System.Domain.Share.ValueObj
{
    public sealed record StandardFamilyCode
    {
        public string Value { get; private set; }

        public StandardFamilyCode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("StandardFamilyCode cannot be null or empty.", nameof(value));

            Value = value;
        }

        public static StandardFamilyCode FromString(string value) => new(value);
        //校验规则，格式要求在此实现
    }
}
