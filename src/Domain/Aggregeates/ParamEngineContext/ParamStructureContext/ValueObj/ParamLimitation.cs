using System.Globalization;
using System.Text.Json.Serialization;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj
{
    public class ParamLimitation
    {
        public string ValueTypeName { get; set; } = "System.String";

        // 运行时获取 Type（不序列化）
        [JsonIgnore]
        public Type ValueType => Type.GetType(ValueTypeName) ?? typeof(string);// 可选：若与 ParamDefinition.ValueType 重复可不设置
        public List<object>? AllowedValues { get; set; } = null;
        public object? Min { get; set; } = null;
        public object? Max { get; set; } = null;

        public ParamLimitation() { }

        /// <summary>
        /// 校验值是否满足限制。若需要类型信息，请传入 fallbackType（通常为 ParamDefinition.ValueType）。
        /// 返回 true 表示通过校验（包括 value == null 时返回 true，null 可否接受由外层根据 ParamDefinition.IsNullable 决定）。
        /// </summary>
        public bool IsValid(object? value, Type? fallbackType = null)
        {
            if (value == null) return true; // 空值由 ParamDefinition.IsNullable 控制

            var type = ValueType ?? fallbackType;

            // 类型兼容性检查（若指定）
            if (type != null)
            {
                try
                {
                    // 尝试做一次类型转换以验证可转换性
                    ConvertToType(value, type);
                }
                catch
                {
                    return false;
                }
            }

            // 白名单检查
            if (AllowedValues != null && AllowedValues.Any())
            {
                // 使用简单相等比较；复杂场景可扩展为类型化比较
                var matched = AllowedValues.Any(av => Equals(av, value) || string.Equals(av?.ToString(), value?.ToString(), StringComparison.OrdinalIgnoreCase));
                if (!matched) return false;
            }

            // 数值范围检查（若 Min/Max 有值且 value 可转为 decimal）
            if ((Min != null || Max != null) && TryConvertToDecimal(value, out var valNum))
            {
                if (Min != null && TryConvertToDecimal(Min, out var minNum) && valNum < minNum) return false;
                if (Max != null && TryConvertToDecimal(Max, out var maxNum) && valNum > maxNum) return false;
            }

            return true;
        }

        private static object? ConvertToType(object value, Type targetType)
        {
            if (targetType.IsAssignableFrom(value.GetType())) return value;

            if (targetType.IsEnum)
            {
                if (Enum.TryParse(targetType, value.ToString(), true, out var enumVal))
                    return enumVal;
                throw new InvalidCastException();
            }

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        private static bool TryConvertToDecimal(object value, out decimal result)
        {
            result = 0m;
            if (value == null) return false;
            try
            {
                if (value is decimal d) { result = d; return true; }
                if (value is double db) { result = Convert.ToDecimal(db); return true; }
                if (value is float f) { result = Convert.ToDecimal(f); return true; }
                if (value is int i) { result = i; return true; }
                if (value is long l) { result = l; return true; }
                if (decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
                {
                    result = parsed; return true;
                }
            }
            catch { }
            return false;
        }
    }
}
