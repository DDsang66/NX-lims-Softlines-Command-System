using System.Text.Json.Serialization;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj
{
    public class ParamDefinition
    {
        public string Name { get; init; } = string.Empty;  // "Ballast"
        // 存储字符串，不存 Type
        public string ValueTypeName { get; init; } = "System.String";

        // 运行时获取 Type（不序列化）
        [JsonIgnore]
        public Type ValueType => Type.GetType(ValueTypeName) ?? typeof(string);

        public string Description { get; init; } = string.Empty;
        public bool IsNullable { get; init; }
        public object DefaultValue { get; init; } = new(); // 补偿机制用
    }
}
