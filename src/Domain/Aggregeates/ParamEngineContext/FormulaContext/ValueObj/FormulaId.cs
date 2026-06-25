namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj
{
    public class FormulaId
    {
        public string Value { get; private set; }

        public FormulaId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("FormulaId required", nameof(value));
            Value = value;
        }

        public override string ToString() => Value;
    }
}
