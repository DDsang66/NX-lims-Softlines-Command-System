using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext
{
    /// <summary>
    /// 条件池聚合根
    /// </summary>
    public sealed class ConditionPool : AggregateRoot
    {
        public ConditionPoolId Id { get; private set; }
        public OrderId SourceId { get; private set; }   // 关联的申请单ID
        public CheckListId CheckListId { get; private set; } = new CheckListId(new Guid());  // 关联的检查单ID
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
        public static ConditionPool Create(
            ConditionPoolId id,
            OrderId sourceId,
            CheckListId checkListId,
            IDictionary<string, object?> initial = null!)
        {
            var pool = new ConditionPool
            {
                Id = id ?? throw new ArgumentNullException(nameof(id)),
                SourceId = sourceId ?? throw new ArgumentNullException(nameof(sourceId)),
                CheckListId = checkListId ?? throw new ArgumentNullException(nameof(checkListId)),
                CreatedAt = DateTime.UtcNow,
                Status = ConditionPoolStatus.Draft
            };

            if (initial != null)
            {
                foreach (var kv in initial)
                {
                    // 可以在这里添加值的验证逻辑
                    pool._conditions[kv.Key] = kv.Value;
                }
            }

            return pool;
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
        public void Update(Dictionary<string, object?> values)
        {
            if (Status != ConditionPoolStatus.Draft)
                throw new InvalidOperationException("Can only submit values in Draft status");

            // 校验字段存在性
            foreach (var fieldName in values.Keys)
            {
                if (!_conditions.ContainsKey(fieldName))
                    throw new ArgumentException($"Unknown field: {fieldName}");
            }

            // 校验必填,所有条件均存在才进行下一步
            if (_conditions != null)
            {
                foreach (var (fieldName, meta) in _conditions)
                {
                    if (!values.ContainsKey(fieldName) || values[fieldName] == null)
                        throw new ArgumentException($"Required field missing: {fieldName}");
                }
            }

            // 覆盖值（清空后填充）
            _conditions.Clear();

            foreach (var (fieldName, value) in values)
            {
                _conditions[fieldName] = value;
            }
        }

        /// <summary>
        /// 根据 fieldName 移除一个条件
        /// </summary>
        /// <param name="fieldName"></param>
        public void Remove(string fieldName) => _conditions.Remove(fieldName);

        /// <summary>
        /// 将条件池状态改为已验证
        /// </summary>
        public void ChangeToValidated() => Status = ConditionPoolStatus.Validated;

        /// <summary>
        /// 将条件池状态改为已过期
        /// </summary>
        public void ChangeToExpired() => Status = ConditionPoolStatus.Expired;


        /// <summary>
        /// 将条件池状态改为草稿
        /// </summary>
        public void ChangeToDraft() => Status = ConditionPoolStatus.Draft;
    }
}
