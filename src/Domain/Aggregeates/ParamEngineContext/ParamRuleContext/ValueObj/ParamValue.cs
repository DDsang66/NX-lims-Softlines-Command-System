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

        public ParamValue(string? strValue, string? notes = null)
        {
            Value = strValue;
            Notes = notes;
        }

        public override string ToString() => Value?.ToString() ?? string.Empty;

        //  新增：支持从 string 隐式转换为 ParamValue
        public static implicit operator ParamValue?(string? strValue)
        {
            if (strValue == null) return null;
            return new ParamValue(strValue);
        }

        //  新增：支持从 ParamValue 隐式转换为 string（根据 ToString 逻辑）
        public static implicit operator string?(ParamValue? paramValue)
        {
            return paramValue?.ToString();
        }
    }
}
