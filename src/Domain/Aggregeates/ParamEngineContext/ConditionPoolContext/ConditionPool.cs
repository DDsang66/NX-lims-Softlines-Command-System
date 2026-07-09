using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ValueObj;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext
{
    /// <summary>
    /// 条件池聚合根
    /// </summary>
    public sealed class ConditionPool : IAggregateRoot
    {
        public ConditionPoolId Id { get; private set; }
        public string SourceId { get; private set; } = string.Empty;  // 关联的申请单ID
        private readonly Dictionary<string, object?> _conditions = new(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyDictionary<string, object?> Conditions => _conditions;
        public DateTime CreatedAt { get; private set; }
        public ConditionPoolStatus Status { get; private set; }  // Draft, Validated, Expired

        private ConditionPool() { }

        /// <summary>
        /// 创建一个条件池
        /// </summary>
        /// <param name="id"></param>
        /// <param name="sourceId"></param>
        /// <param name="initial"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public ConditionPool(ConditionPoolId id, string sourceId, IDictionary<string, object?> initial = null!)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            SourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId));
            if (initial != null)
            {
                foreach (var kv in initial) _conditions[kv.Key] = kv.Value;
            }
        }

        /// <summary>
        /// 验证条件池中的条件是否存在
        /// </summary>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public bool HasCondition(string fieldName) => _conditions.ContainsKey(fieldName);

        /// <summary>
        /// 根据 fieldName 获取条件值，如果不存在则抛出异常
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        /// <exception cref="KeyNotFoundException"></exception>
        public T GetConditionValue<T>(string fieldName)
        {
            if (!_conditions.TryGetValue(fieldName, out var v)) throw new KeyNotFoundException($"Condition '{fieldName}' not found");
            return (T)Convert.ChangeType(v, typeof(T));
        }

        /// <summary>
        /// 向条件池中添加或更新一个条件值，如果 fieldName 已存在则覆盖原值，否则新增一个条件
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        /// <exception cref="ArgumentException"></exception>
        public void AddOrUpdate(string fieldName, object? value)
        {
            if (string.IsNullOrWhiteSpace(fieldName)) throw new ArgumentException(nameof(fieldName));
            _conditions[fieldName] = value;
        }

        /// <summary>
        /// 根据 fieldName 移除一个条件
        /// </summary>
        /// <param name="fieldName"></param>
        public void Remove(string fieldName) => _conditions.Remove(fieldName);
    }
}
