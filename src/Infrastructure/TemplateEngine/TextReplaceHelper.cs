using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine
{
    /// <summary>
    /// 模板填充助手
    /// 支持：
    /// 1. 简单字段 {Name}
    /// 2. 嵌套路径 {Product.Name}
    /// 3. 数组索引 {Orders[0].Id}
    /// 4. ParamValue 自动拆包（{X} 取 X.Value；{X.Value} / {X.Unit} 也可单独取）
    /// 5. 大小写不敏感匹配
    /// 6. 数组自动 join
    /// </summary>
    public static class TextReplaceHelper
    {
        /// <summary>
        /// 数组默认分隔符
        /// </summary>
        private const string ArraySeparator = ", ";

        /// <summary>
        /// ParamValue 拆包时使用的候选字段名（大小写不敏感）
        /// </summary>
        private static readonly string[] ValueFieldNames = { "Value" };
        private static readonly string[] UnitFieldNames = { "Unit" };

        /// <summary>
        /// 用 JSON 字符串填充模板
        /// </summary>
        public static string FillTemplate(string template, string json)
        {
            if (string.IsNullOrEmpty(template)) return template;
            if (string.IsNullOrEmpty(json)) return template;

            using var doc = JsonDocument.Parse(json);
            return FillTemplate(template, doc.RootElement);
        }

        /// <summary>
        /// 用 JsonElement 填充模板
        /// </summary>
        public static string FillTemplate(string template, JsonElement jsonElement)
        {
            if (string.IsNullOrEmpty(template)) return template;

            // 缓存，避免同一路径多次解析
            var cache = new Dictionary<string, string>(StringComparer.Ordinal);

            jsonElement = Unwrap(jsonElement);

            return Regex.Replace(template, @"\{([^{}]+)\}", match =>
            {
                string path = match.Groups[1].Value.Trim();

                if (cache.TryGetValue(path, out var cached))
                    return cached;

                var value = GetValueByPath(jsonElement, path);
                // 找不到时保留原占位符
                var result = value ?? match.Value;
                cache[path] = result;
                return result;
            });
        }

        // ================================================================
        //  路径解析 + 取值
        // ================================================================

        /// <summary>
        /// 剥掉常见的包装层：{ "values": { ... } } → { ... }
        /// 只往下钻一层或几层，直到遇到"非包装"对象
        /// </summary>
        private static JsonElement Unwrap(JsonElement element)
        {
            const int MaxDepth = 5;   // 防止死循环
            int depth = 0;

            while (depth++ < MaxDepth)
            {
                if (element.ValueKind != JsonValueKind.Object) break;

                // 情况 1：对象只有 1 个属性，且属性名是 values/value/data 之类
                // 情况 2：对象有属性 "values" 且它也是对象
                JsonElement next = default;
                bool found = false;
                string? foundKey = null;

                foreach (var prop in element.EnumerateObject())
                {
                    if (IsWrapperKey(prop.Name) && prop.Value.ValueKind == JsonValueKind.Object)
                    {
                        next = prop.Value;
                        found = true;
                        foundKey = prop.Name;
                        break;
                    }
                }

                // 只有当这个对象"主要就是包装"时才钻，避免误伤
                // 判断标准：对象属性数 <= 2（留点容错，比如可能有 extra 字段）
                int propCount = 0;
                foreach (var _ in element.EnumerateObject()) propCount++;

                if (found && propCount <= 2)
                {
                    element = next;
                    continue;
                }

                break;
            }

            return element;
        }

        private static bool IsWrapperKey(string name)
        {
            return name.Equals("values", StringComparison.OrdinalIgnoreCase)
                || name.Equals("value", StringComparison.OrdinalIgnoreCase)
                || name.Equals("data", StringComparison.OrdinalIgnoreCase)
                || name.Equals("items", StringComparison.OrdinalIgnoreCase);
        }

        private static string? GetValueByPath(JsonElement element, string path)
        {
            try
            {
                var tokens = ParsePath(path);
                JsonElement current = element;

                foreach (var token in tokens)
                {
                    if (token.IsIndex)
                    {
                        if (current.ValueKind != JsonValueKind.Array) return null;
                        int index = token.Index;
                        if (index < 0 || index >= current.GetArrayLength()) return null;
                        current = current[index];
                    }
                    else
                    {
                        if (current.ValueKind != JsonValueKind.Object) return null;
                        if (!TryGetPropertyIgnoreCase(current, token.Name, out var next)) return null;
                        current = next;
                    }
                }

                return ElementToString(current);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 大小写不敏感地获取 object 属性
        /// </summary>
        private static bool TryGetPropertyIgnoreCase(JsonElement obj, string name, out JsonElement value)
        {
            // 先按原名精确匹配（性能最好）
            if (obj.TryGetProperty(name, out value)) return true;

            // 再按忽略大小写遍历
            foreach (var prop in obj.EnumerateObject())
            {
                if (string.Equals(prop.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = prop.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        // ================================================================
        //  JsonElement -> string（含 ParamValue 拆包、数组 join）
        // ================================================================

        private static string ElementToString(JsonElement element)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    return element.GetString() ?? string.Empty;

                case JsonValueKind.Number:
                    return element.GetRawText();

                case JsonValueKind.True:
                    return "true";

                case JsonValueKind.False:
                    return "false";

                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return string.Empty;

                case JsonValueKind.Object:
                    return ExtractFromObject(element);

                case JsonValueKind.Array:
                    return ExtractFromArray(element);

                default:
                    return element.GetRawText();
            }
        }

        /// <summary>
        /// 对象拆包：
        /// - 如果对象包含 Value 字段（ParamValue 结构），取 Value 的值
        /// - 否则原样返回 JSON 文本
        /// </summary>
        private static string ExtractFromObject(JsonElement element)
        {
            // 尝试取 Value 字段（大小写不敏感）
            foreach (var name in ValueFieldNames)
            {
                if (TryGetPropertyIgnoreCase(element, name, out var valueElement))
                {
                    // 递归：Value 可能还是复杂类型
                    return ElementToString(valueElement);
                }
            }

            // 没有 Value 字段 → 不是 ParamValue，原样返回
            return element.GetRawText();
        }

        /// <summary>
        /// 数组 join
        /// </summary>
        private static string ExtractFromArray(JsonElement element)
        {
            if (element.GetArrayLength() == 0) return string.Empty;

            var items = new List<string>(element.GetArrayLength());
            foreach (var item in element.EnumerateArray())
            {
                items.Add(ElementToString(item));
            }
            return string.Join(ArraySeparator, items);
        }

        // ================================================================
        //  路径解析
        // ================================================================

        private struct PathToken
        {
            public bool IsIndex;
            public string Name;
            public int Index;
        }

        /// <summary>
        /// 解析路径：a.b[0].c  => [a, b, [0], c]
        /// </summary>
        private static List<PathToken> ParsePath(string path)
        {
            var tokens = new List<PathToken>();
            int i = 0;
            var sb = new StringBuilder();

            void FlushName()
            {
                if (sb.Length > 0)
                {
                    tokens.Add(new PathToken { IsIndex = false, Name = sb.ToString() });
                    sb.Clear();
                }
            }

            while (i < path.Length)
            {
                char c = path[i];

                if (c == '.')
                {
                    FlushName();
                    i++;
                }
                else if (c == '[')
                {
                    FlushName();
                    int end = path.IndexOf(']', i);
                    if (end < 0) break;

                    string idxStr = path.Substring(i + 1, end - i - 1).Trim();
                    if (int.TryParse(idxStr, out int idx))
                    {
                        tokens.Add(new PathToken { IsIndex = true, Index = idx });
                    }
                    i = end + 1;
                }
                else
                {
                    sb.Append(c);
                    i++;
                }
            }

            FlushName();
            return tokens;
        }
    }
}