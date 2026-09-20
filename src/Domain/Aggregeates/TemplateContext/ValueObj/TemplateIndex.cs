using NX_lims_Softlines_Command_System.src.Domain.Share;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext.ValueObj
{
    /// <summary>
    /// 该类表示一个模板索引，包含模板索引需要的条件和条件值，一般认为只要满足索引内的key:value就可以直接索引至该模板。
    /// </summary>
    public class TemplateIndex: ValueObject
    {
        private readonly Dictionary<string, object?> _values;

        public IReadOnlyDictionary<string, object?> Values => _values;

        // EF Core 需要
        private TemplateIndex()
        {
            _values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        private TemplateIndex(Dictionary<string, object?> values)
        {
            _values = new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 创建模板索引
        /// </summary>
        public static TemplateIndex Create(IEnumerable<KeyValuePair<string, object?>> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var kvp in values)
            {
                if (string.IsNullOrWhiteSpace(kvp.Key))
                    throw new ArgumentException("索引键不能为空", nameof(values));

                if (dict.ContainsKey(kvp.Key))
                    throw new ArgumentException($"索引键重复: {kvp.Key}", nameof(values));

                dict[kvp.Key] = kvp.Value;
            }

            if (dict.Count == 0)
                throw new ArgumentException("模板索引不能为空", nameof(values));

            return new TemplateIndex(dict);
        }

        /// <summary>
        /// 创建一个空索引（用于后续逐步添加）
        /// </summary>
        public static TemplateIndex Empty()
        {
            return new TemplateIndex(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 添加或更新一个索引项（返回新实例，保持不可变性）
        /// </summary>
        public TemplateIndex AddOrUpdate(string key, object? value)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("索引键不能为空", nameof(key));

            var newDict = new Dictionary<string, object?>(_values, StringComparer.OrdinalIgnoreCase)
            {
                [key] = value
            };

            return new TemplateIndex(newDict);
        }

        /// <summary>
        /// 移除一个索引项（返回新实例）
        /// </summary>
        public TemplateIndex Remove(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("索引键不能为空", nameof(key));

            if (!_values.ContainsKey(key))
                return this;

            var newDict = new Dictionary<string, object?>(_values, StringComparer.OrdinalIgnoreCase);
            newDict.Remove(key);

            return new TemplateIndex(newDict);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            // 按 key 排序后比较，保证相等性判断稳定
            foreach (var kvp in _values.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                yield return kvp.Key.ToLowerInvariant();
                yield return kvp.Value ?? string.Empty;
            }
        }

        /// <summary>
        /// 序列化为 JSON（供 EF Core ValueConverter 用）
        /// </summary>
        public string ToJson()
        {
            return JsonSerializer.Serialize(_values, (JsonSerializerOptions)null);
        }

        /// <summary>
        /// 从 JSON 反序列化（供 EF Core ValueConverter 用）
        /// 把 JsonElement 还原成 CLR 基础类型，保证 Matches 里的类型比较正确
        /// </summary>
        public static TemplateIndex FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return Empty();

            using var doc = JsonDocument.Parse(json);
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString(),
                    JsonValueKind.Number => prop.Value.TryGetInt64(out var l) ? l : prop.Value.GetDecimal(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => prop.Value.GetRawText()
                };
            }

            return new TemplateIndex(dict);
        }
    }
}
