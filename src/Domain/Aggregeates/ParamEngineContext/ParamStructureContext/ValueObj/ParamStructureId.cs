using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj
{
    public class ParamStructureId : AggregateRootId
    {
        public string Value { get; }

        public ParamStructureId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("ParamStructureId is required", nameof(value));
            Value = value;
        }

        public override string ToString() => Value.ToString();

        public override bool Equals(object? obj) => Equals(obj as ParamStructureId);

        public bool Equals(ParamStructureId? other) => other != null && string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
    }
}
