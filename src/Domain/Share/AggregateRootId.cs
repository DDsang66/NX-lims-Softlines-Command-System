using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Events;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Share
{
    public abstract class AggregateRootId<TId> : IAggregateRootId<TId>
        where TId : notnull
    {
        public TId Value { get; }

        protected AggregateRootId(TId value)
        {
            // 防止传入 null 值（虽然 where TId : notnull 已经做了值类型约束，但引用类型仍可能传 null）
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        // 使用 override 提供默认实现，并 sealed 防止子类意外改变 ID 的字符串表现形式
        public sealed override string ToString() => Value.ToString()!;

        public override int GetHashCode() => Value.GetHashCode();

        // 重写 Equals 方法，确保基于 Value 的值相等性比较
        public override bool Equals(object? obj)
        {
            if (obj is not AggregateRootId<TId> other)
            {
                return false;
            }

            return Equals(other);
        }

        // 实现 IEquatable<T> 的强类型 Equals 方法
        public bool Equals(IAggregateRootId<TId>? other)
        {
            if (other is null)
            {
                return false;
            }

            // 处理 Value 是 string 类型时的特殊情况，确保使用序数比较
            if (Value is string strValue && other.Value is string otherStrValue)
            {
                return string.Equals(strValue, otherStrValue, StringComparison.Ordinal);
            }

            return EqualityComparer<TId>.Default.Equals(Value, other.Value);
        }

        // 重载 == 和 != 运算符，提供更自然的比较方式
        public static bool operator ==(AggregateRootId<TId>? left, AggregateRootId<TId>? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(AggregateRootId<TId>? left, AggregateRootId<TId>? right)
        {
            return !Equals(left, right);
        }
    }
}
