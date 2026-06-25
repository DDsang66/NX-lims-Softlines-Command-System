namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj
{
    public class ParamDefinition
    {
        public string Name { get; set; }  // "Ballast"
        public Type ValueType { get; set; }  // typeof(string)
        public string Description { get; set; }
        public bool IsNullable { get; set; }
        public object DefaultValue { get; set; }  // 补偿机制用
    }
}
