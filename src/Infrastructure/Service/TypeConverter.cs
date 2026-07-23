using System.Text.Json;
using System.Text.Json.Serialization;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Service
{
    /// <summary>
    /// 自定义 Type 类型的 JSON 转换器
    /// </summary>
    public class TypeConverter : JsonConverter<Type>
    {
        public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType != JsonTokenType.String)
            {
                throw new JsonException($"Expected string token for Type, but got {reader.TokenType}.");
            }

            var typeName = reader.GetString();
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            // 尝试从当前 AppDomain 的所有程序集中查找类型
            var type = Type.GetType(typeName, throwOnError: false, ignoreCase: false)
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType(typeName, throwOnError: false, ignoreCase: false))
                    .FirstOrDefault(t => t != null);

            if (type == null)
            {
                throw new JsonException($"Could not resolve type: '{typeName}'. Ensure the type name is fully qualified and the assembly is loaded.");
            }

            return type;
        }

        public override void Write(Utf8JsonWriter writer, Type? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(value.AssemblyQualifiedName);
        }

        public override Type ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var typeName = reader.GetString()
                ?? throw new JsonException("Type name cannot be null when used as dictionary key.");

            var type = Type.GetType(typeName, throwOnError: false, ignoreCase: false)
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType(typeName, throwOnError: false, ignoreCase: false))
                    .FirstOrDefault(t => t != null)
                ?? throw new JsonException($"Could not resolve type: '{typeName}'.");

            return type;
        }

        public override void WriteAsPropertyName(Utf8JsonWriter writer, Type? value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                throw new JsonException("Cannot write null Type as dictionary key.");
            }

            writer.WritePropertyName(value.AssemblyQualifiedName);
        }
    }
}
