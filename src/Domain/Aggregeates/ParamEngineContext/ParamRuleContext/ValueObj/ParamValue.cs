namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj
{
    public class ParamValue
    {
        public object? Value { get; set; }
        public string? Notes { get; set; }

        public ParamValue() { }

        public ParamValue(object? value, string? notes = null)
        {
            Value = value;
            Notes = notes;
        }

        public override string ToString() => Value?.ToString() ?? string.Empty;
    }
}
