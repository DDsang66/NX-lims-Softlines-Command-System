using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj
{
    public class StandardId : AggregateRootId
    {
        public string Value { get; }

        public StandardId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));

            if (value.Length > 50)
                throw new ArgumentException("IdStandard cannot exceed 50 characters.", nameof(value));
            Value = value;
        }

        // 2. 显式重写 ToString，解决输出 {IdStandard { Value = ... }} 的问题
        public override string ToString() => Value.ToString();

        public bool Equals(StandardId? other) => other != null && string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
    }
}
