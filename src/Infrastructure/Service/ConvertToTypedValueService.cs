using System.Globalization;
using System.Text.Json;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Service
{
    public class ConvertToTypedValueService : IScopedDependency
    {
        /// <summary>
        /// 主入口：将任意输入（string / JsonElement / object）转换为指定类型的对象
        /// </summary>
        public static object? Convert(object? value, string paramTypeName)
        {
            // 1. 统一提取字符串表示
            var stringValue = ExtractString(value);
            if (stringValue is null) return null;

            // 2. 按类型转换
            return ParseTypedValue(stringValue, paramTypeName);
        }

        /// <summary>
        /// 字符串专用入口（向后兼容）
        /// </summary>
        public static object? ConvertToTypedValue(string? stringValue, string paramTypeName)
        {
            if (stringValue is null) return null;
            return ParseTypedValue(stringValue, paramTypeName);
        }

        /// <summary>
        /// JsonElement 专用入口（向后兼容）
        /// </summary>
        public static object? ConvertJsonElement(JsonElement element, string typeName)
        {
            var stringValue = ExtractString(element);
            if (stringValue is null) return null;
            return ParseTypedValue(stringValue, typeName);
        }

        // ==================== 私有方法 ====================

        /// <summary>
        /// 从各种输入类型中提取字符串表示
        /// </summary>
        private static string? ExtractString(object? value)
        {
            return value switch
            {
                null => null,
                string s => s,
                JsonElement { ValueKind: JsonValueKind.Undefined or JsonValueKind.Null } => null,
                JsonElement { ValueKind: JsonValueKind.String } el => el.GetString(),
                JsonElement { ValueKind: JsonValueKind.Number } el => el.GetRawText(),
                JsonElement { ValueKind: JsonValueKind.True } => "true",
                JsonElement { ValueKind: JsonValueKind.False } => "false",
                JsonElement el => el.GetRawText(),
                _ => value.ToString()
            };
        }

        /// <summary>
        /// 核心：将字符串按类型名解析为对应 .NET 对象
        /// </summary>
        private static object? ParseTypedValue(string value, string paramTypeName)
        {
            return paramTypeName switch
            {
                "System.String" => value,

                "System.Int32" => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec)
                    && dec == Math.Truncate(dec)  // 确保是整数（如 2.0）
                    ? (int)dec
                    : null,
                "System.Int64" => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec)
                   && dec == Math.Truncate(dec)
                   ? (long)dec
                   : null,

                "System.Decimal" => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null,
                "System.Double" => double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null,
                "System.Single" => float.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : null,

                "System.Boolean" => bool.TryParse(value, out var v) ? v : null,

                "System.DateTime" => DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var v) ? v : null,
                "System.DateTimeOffset" => DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var v) ? v : null,

                "System.Guid" => Guid.TryParse(value, out var v) ? v : null,

                _ => value // 未知类型按 string 处理
            };
        }
    }
}