using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj
{
    public class ParamRuleId:IAggregateRootId
    {
        public string Value { get; }
        public ParamRuleId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("ParamRuleId required", nameof(value));
            Value = value;
        }
        public override string ToString() => Value;
    }
}
