namespace NX_lims_Softlines_Command_System.src.Domain.Share
{
    /// <summary>
    /// DDD 值对象基类
    /// 按属性值判断相等，无身份标识
    public abstract class ValueObject : IEquatable<ValueObject>
    {
        protected abstract IEnumerable<object> GetEqualityComponents();

        public override bool Equals(object? obj)
        {
            if (obj is not ValueObject other) return false;
            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        public bool Equals(ValueObject? other) => Equals((object?)other);

        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Aggregate(0, (hash, component) =>
                    HashCode.Combine(hash, component?.GetHashCode() ?? 0));
        }

        public static bool operator ==(ValueObject? left, ValueObject? right)
        {
            if (left is null && right is null) return true;
            if (left is null || right is null) return false;
            return left.Equals(right);
        }

        public static bool operator !=(ValueObject? left, ValueObject? right)
            => !(left == right);
    }
}

