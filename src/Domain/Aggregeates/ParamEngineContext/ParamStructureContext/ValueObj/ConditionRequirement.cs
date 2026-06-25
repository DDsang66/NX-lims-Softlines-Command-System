namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj
{
    public class ConditionRequirement
    {
        public string FieldName { get; set; } = string.Empty;  // "FiberDominantType"
        public Type FieldType { get; set; } = typeof(string);   // typeof(string)
        public bool IsRequired { get; set; }
        public List<object> AllowedValues { get; set; } = new List<object>();  // 可选值的白名单，这里是指条件可选值
    }
}
