namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ValueObj
{
    public class ConditionPoolId
    {
        public string Value { get; private set; }
        public ConditionPoolId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("ConditionPoolId required", nameof(value));
            Value = value;
        }
        public override string ToString() => Value;
    }
}
