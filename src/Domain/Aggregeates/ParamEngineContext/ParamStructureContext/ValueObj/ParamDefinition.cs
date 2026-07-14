using System.Text.Json.Serialization;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj
{
    public class ParamDefinition
    {
        public string Name { get; set; } = string.Empty;  // "Ballast"
        // 存储字符串，不存 Type
        public string ValueTypeName { get; set; } = "System.String";

        // 运行时获取 Type（不序列化）
        [JsonIgnore]
        public Type ValueType => Type.GetType(ValueTypeName) ?? typeof(string);

        public string Description { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
        public object DefaultValue { get; set; } = new(); // 补偿机制用
    }
}
