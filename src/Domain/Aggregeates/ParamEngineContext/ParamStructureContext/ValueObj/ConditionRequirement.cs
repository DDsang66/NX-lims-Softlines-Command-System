using System.Text.Json.Serialization;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj
{
    public class ConditionRequirement
    {
        public string FieldName { get; init; } = string.Empty;  // "FiberContent这样的基础字段名(一般用于一级条件池的构建)"
        public string ValueTypeName { get; init; } = "System.String";

        // 运行时获取 Type（不序列化）
        [JsonIgnore]
        public Type ValueType => Type.GetType(ValueTypeName) ?? typeof(string); // List<string>、string、int、double、bool 等
        public bool IsRequired { get; init; }
        public List<object> AllowedValues { get; init; } = new List<object>();  // 可选值的白名单，这里是指条件可选值
    }
}
