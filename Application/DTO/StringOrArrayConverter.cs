using System.Text.Json;
using System.Text.Json.Serialization;

namespace NX_lims_Softlines_Command_System.Application.DTO
{
    public class StringOrArrayConverter : JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    return reader.GetString();

                case JsonTokenType.StartArray:
                    var list = new List<string>();
                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonTokenType.EndArray)
                            break;
                        if (reader.TokenType == JsonTokenType.String)
                            list.Add(reader.GetString()!);
                    }
                    return string.Join(",", list);

                case JsonTokenType.Null:
                    return null;

                default:
                    throw new JsonException($"Unsupported token type: {reader.TokenType}");
            }
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }
}
