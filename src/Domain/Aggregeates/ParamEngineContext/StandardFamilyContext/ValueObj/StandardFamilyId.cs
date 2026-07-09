using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj
{
    public class StandardFamilyId:IAggregateRootId
    {
        public string Value { get; }

        public StandardFamilyId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("ParamStructureId is required", nameof(value));
            Value = value;
        }

        public override string ToString() => Value;

        public override bool Equals(object? obj) => Equals(obj as StandardFamilyId);

        public bool Equals(StandardFamilyId? other) => other != null && string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
    }
}
