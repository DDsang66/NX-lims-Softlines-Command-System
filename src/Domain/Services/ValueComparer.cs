using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Conparison;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using System;
using System.Collections;
using System.Globalization;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Domain.Services
{
    /// <summary>
    /// 值比较器
    /// </summary>
    public class ValueComparer : IValueComparer, IScopedDependency
    {
        /// <summary>
        /// 等值比较（支持数字、字符串、布尔值等类型的宽松比较）
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public bool AreEqual(object? a, object? b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            // JsonElement 先提取实际值
            a = UnwrapJsonElement(a);
            b = UnwrapJsonElement(b);

            if (TryConvertToDecimal(a, out var da) && TryConvertToDecimal(b, out var db))
                return da == db;

            if (a is string sa && b is string sb)
                return string.Equals(sa.Trim(), sb.Trim(), StringComparison.OrdinalIgnoreCase);

            return Equals(a, b);
        }

        /// <summary>
        /// 比较两个值，根据指定的比较操作符返回结果
        /// </summary>
        /// <param name="a">第一个值</param>
        /// <param name="op">比较操作符</param>
        /// <param name="b">第二个值</param>
        /// <returns>比较结果</returns>
        public bool Compare(object? a, ComparisonOperator op, object? b)
        {
            if (a == null || b == null) return false;

            if (TryConvertToDecimal(a, out var da) && TryConvertToDecimal(b, out var db))
            {
                return op switch
                {
                    ComparisonOperator.Equal => da == db,
                    ComparisonOperator.NotEqual => da != db,
                    ComparisonOperator.GreaterThan => da > db,
                    ComparisonOperator.GreaterThanOrEqual => da >= db,
                    ComparisonOperator.LessThan => da < db,
                    ComparisonOperator.LessThanOrEqual => da <= db,
                    _ => false
                };
            }

            if (bool.TryParse(a.ToString(), out var ab) && bool.TryParse(b.ToString(), out var bb))
            {
                return op switch
                {
                    ComparisonOperator.Equal => ab == bb,
                    ComparisonOperator.NotEqual => ab != bb,
                    _ => false
                };
            }

            var asStr = a.ToString();
            var bsStr = b.ToString();
            if (asStr != null && bsStr != null)
            {
                var cmp = string.Compare(asStr.Trim(), bsStr.Trim(), StringComparison.OrdinalIgnoreCase);
                return op switch
                {
                    ComparisonOperator.Equal => cmp == 0,
                    ComparisonOperator.NotEqual => cmp != 0,
                    ComparisonOperator.GreaterThan => cmp > 0,
                    ComparisonOperator.GreaterThanOrEqual => cmp >= 0,
                    ComparisonOperator.LessThan => cmp < 0,
                    ComparisonOperator.LessThanOrEqual => cmp <= 0,
                    _ => false
                };
            }

            if (a is IComparable ac)
            {
                try
                {
                    var cmpv = ac.CompareTo(b);
                    return op switch
                    {
                        ComparisonOperator.Equal => cmpv == 0,
                        ComparisonOperator.NotEqual => cmpv != 0,
                        ComparisonOperator.GreaterThan => cmpv > 0,
                        ComparisonOperator.GreaterThanOrEqual => cmpv >= 0,
                        ComparisonOperator.LessThan => cmpv < 0,
                        ComparisonOperator.LessThanOrEqual => cmpv <= 0,
                        _ => false
                    };
                }
                catch { return false; }
            }

            return false;
        }

        /// <summary>
        /// 尝试将对象转换为十进制数
        /// </summary>
        /// <param name="v">要转换的对象</param>
        /// <param name="d">转换后的十进制数</param>
        /// <returns>转换是否成功</returns>
        public bool TryConvertToDecimal(object? v, out decimal d)
        {
            d = 0m;
            if (v == null) return false;

            // JsonElement 先解包
            if (v is JsonElement je)
            {
                v = je.ValueKind switch
                {
                    JsonValueKind.Number => je.GetDecimal(),
                    JsonValueKind.String => je.GetString(),
                    _ => null
                };
                if (v == null) return false;
            }

            return v switch
            {
                decimal dec => (d = dec) == d,
                double db => (d = (decimal)db) == d,
                float f => (d = (decimal)f) == d,
                int i => (d = i) == d,
                long l => (d = l) == d,
                string s => decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out d),
                IConvertible => decimal.TryParse(Convert.ToString(v), NumberStyles.Any, CultureInfo.InvariantCulture, out d),
                _ => false
            };
        }

        /// <summary>
        /// 解包 JsonElement 为实际值
        /// </summary>
        private object? UnwrapJsonElement(object? value)
        {
            if (value is not JsonElement jsonElement)
                return value;

            return jsonElement.ValueKind switch
            {
                JsonValueKind.String => jsonElement.GetString(),
                JsonValueKind.Number => jsonElement.TryGetDecimal(out var d) ? d : jsonElement.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => value  // 其他类型保持原样
            };
        }

        /// <summary>
        /// 判断对象是否为真值
        /// </summary>
        /// <param name="v">要判断的对象</param>
        /// <returns>对象是否为真值</returns>
        public bool IsTruthy(object? v)
        {
            if (v == null) return false;
            if (v is bool b) return b;
            if (v is string s) return !string.IsNullOrWhiteSpace(s) && !string.Equals(s, "false", StringComparison.OrdinalIgnoreCase);
            if (v is IEnumerable e) return e.Cast<object?>().Any();
            return true;
        }
    }
}
