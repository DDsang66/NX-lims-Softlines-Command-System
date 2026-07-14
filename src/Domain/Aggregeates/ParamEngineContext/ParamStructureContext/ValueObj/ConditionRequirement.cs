using System.Text.Json.Serialization;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj
{
    public class ConditionRequirement
    {
        public string FieldName { get; set; } = string.Empty;  // "FiberContent这样的基础字段名(一般用于一级条件池的构建)"
        public string ValueTypeName { get; set; } = "System.String";

        // 运行时获取 Type（不序列化）
        [JsonIgnore]
        public Type ValueType => Type.GetType(ValueTypeName) ?? typeof(string); // List<string>、string、int、double、bool 等
        public bool IsRequired { get; set; }
        public List<object> AllowedValues { get; set; } = new List<object>();  // 可选值的白名单，这里是指条件可选值
    }
}
