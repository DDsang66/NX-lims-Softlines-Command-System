using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository.EventOutBoxUtil
{
    /// <summary>
    /// IAggregateRootId&lt;TValue&gt; 的 JSON 转换器。
    ///
    /// 为什么需要它：
    ///   IAggregateRootId&lt;TValue&gt; 是接口，System.Text.Json 默认无法反序列化接口
    ///   （不知道该 new 哪个具体类）。本转换器在序列化时额外写出 $type（具体类型限定名），
    ///   反序列化时按 $type 重建具体 ID 对象。
    ///
    /// 输出格式：
    ///   { "$type": "命名空间.TestItemId, 程序集", "Value": "xxxxxxxx-...." }
    ///
    /// 依赖约定：
    ///   每个具体 IAggregateRootId&lt;TValue&gt; 实现类必须有 public 的 (TValue value) 构造器。
    /// </summary>
    public sealed class AggregateRootIdJsonConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsGenericType
                && typeToConvert.GetGenericTypeDefinition() == typeof(IAggregateRootId<>);
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var valueType = typeToConvert.GetGenericArguments()[0];
            var converterType = typeof(InnerConverter<>).MakeGenericType(valueType);
            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }

        private sealed class InnerConverter<TValue> : JsonConverter<IAggregateRootId<TValue>>
            where TValue : notnull
        {
            public override IAggregateRootId<TValue>? Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                    return null;

                using var doc = JsonDocument.ParseValue(ref reader);
                var root = doc.RootElement;

                if (!root.TryGetProperty("$type", out var typeProp))
                    throw new JsonException("IAggregateRootId 反序列化缺少 $type 字段。");

                var typeName = typeProp.GetString();
                if (string.IsNullOrWhiteSpace(typeName))
                    throw new JsonException("IAggregateRootId 反序列化的 $type 为空。");

                var idType = ResolveType(typeName!)
                    ?? throw new JsonException($"无法解析 IAggregateRootId 具体类型：{typeName}");

                if (!root.TryGetProperty("Value", out var valueProp))
                    throw new JsonException("IAggregateRootId 反序列化缺少 Value 字段。");

                var valueJson = valueProp.GetRawText();
                var value = JsonSerializer.Deserialize(valueJson, typeof(TValue), options);

                // 具体 ID 类必须有 (TValue) 构造器
                var instance = Activator.CreateInstance(idType, value)
                    ?? throw new JsonException($"无法构造 IAggregateRootId 实例：{idType.FullName}");

                return (IAggregateRootId<TValue>)instance;
            }

            public override void Write(
                Utf8JsonWriter writer,
                IAggregateRootId<TValue> value,
                JsonSerializerOptions options)
            {
                if (value is null)
                {
                    writer.WriteNullValue();
                    return;
                }

                writer.WriteStartObject();
                writer.WriteString("$type", value.GetType().AssemblyQualifiedName);
                writer.WritePropertyName("Value");
                JsonSerializer.Serialize(writer, value.Value, options);
                writer.WriteEndObject();
            }

            private static Type? ResolveType(string typeName)
            {
                var t = Type.GetType(typeName);
                if (t != null) return t;

                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    t = asm.GetType(typeName);
                    if (t != null) return t;
                }
                return null;
            }
        }
    }
}
