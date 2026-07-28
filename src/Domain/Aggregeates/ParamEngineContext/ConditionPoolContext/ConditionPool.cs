using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext
{
    /// <summary>
    /// 条件池聚合根
    /// </summary>
    public sealed class ConditionPool : AggregateRoot<ConditionPoolId,Guid>
    {
        /// <summary>
        /// 关联的检测清单ID
        /// </summary>
        public CheckListId CheckListId { get; private set; } = new CheckListId(Guid.NewGuid());

        /// <summary>
        /// 条件池中的条件
        /// </summary>
        private readonly Dictionary<string, object?> _conditions = new(StringComparer.OrdinalIgnoreCase);
        public IReadOnlyDictionary<string, object?> Conditions => _conditions;

        /// <summary>
        /// 使用此条件池的测点ID列表
        /// </summary>
        public ISet<string> TestPoints { get; private set; } = new HashSet<string>();

        /// <summary>
        /// 条件池的创建时间
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// 条件池的状态
        /// </summary>
        public ConditionPoolStatus Status { get; private set; }  // Draft, Validated, Expired

        /// <summary>
        /// 创建一个条件池
        /// </summary>
        /// <param name="id"></param>
        /// <param name="sourceId"></param>
        /// <param name="initial"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static ConditionPool Create(
            CheckListId checkListId,
            IDictionary<string, object?> initial = null!)
        {
            var pool = new ConditionPool
            {
                Id = new ConditionPoolId(Guid.NewGuid()),
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
        /// 重建
        /// </summary>
        /// <param name="id"></param>
        /// <param name="checkListId"></param>
        /// <param name="initial"></param>
        /// <param name="createdAt"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ConditionPool Reconstitute(
            ConditionPoolId id,
            CheckListId checkListId,
            IDictionary<string, object?> initial,
            ISet<string> testPoints,
            DateTime createdAt,
            ConditionPoolStatus status
            ) 
        {
            var pool = new ConditionPool
            {
                Id = id,
                CheckListId = checkListId ?? throw new ArgumentNullException(nameof(checkListId)),
                TestPoints = testPoints,
                CreatedAt = createdAt,
                Status = status
            };

            foreach (var kv in initial)
            {
                // 可以在这里添加值的验证逻辑
                pool._conditions[kv.Key] = kv.Value;
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
            if (!_conditions.TryGetValue(fieldName, out var v))
                throw new KeyNotFoundException($"Condition '{fieldName}' not found");

            if (v == null)
                return default!;

            // 已经是目标类型
            if (v is T typed)
                return typed;

            // JSON 序列化再反序列化（处理 JsonElement 等中间类型）
            var json = JsonSerializer.Serialize(v, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? throw new InvalidOperationException($"Failed to deserialize '{fieldName}' to {typeof(T).Name}");
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

                // 覆盖值（清空后填充）
                _conditions.Clear();

                foreach (var (fieldName, value) in values)
                {
                    _conditions[fieldName] = value;
                }
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


        /// <summary>
        /// 将条件池状态改为已提交
        /// </summary>
        /// <param name="conditions"></param>
        /// <param name="testPoints"></param>
        public void MergeFrom(Dictionary<string, object?> conditions, List<string> testPoints)
        {
            // 内部处理非空判断和合并逻辑
            if (conditions?.Any() == true)
            {
                Update(conditions);
            }

            AddTestPoints(testPoints);
        }

        /// <summary>
        /// 添加测点到条件池
        /// </summary>
        /// <param name="testPoint">测点名称列表</param>
        public void AddTestPoints(IEnumerable<string> testPoints)
        {
            foreach (var testPoint in testPoints)
            {
                if (string.IsNullOrWhiteSpace(testPoint))
                {
                    throw new ArgumentException("测点名称不能为空", nameof(testPoint));
                }

                TestPoints.Add(testPoint);
            }
        }

        /// <summary>
        /// 从条件池中移除测点
        /// </summary>
        /// <param name="testPoints">测点名称</param>
        public void RemoveTestPoint(string testPoint)
        {
            if (string.IsNullOrWhiteSpace(testPoint))
            {
                throw new ArgumentException("测点名称不能为空", nameof(testPoint));
            }

            TestPoints.Remove(testPoint);
        }

        /// <summary>
        /// 判断条件池是否包含指定测点
        /// </summary>
        /// <param name="testPointName">测点名称</param>
        /// <returns>是否包含</returns>
        public bool ContainsTestPoint(string testPoint)
        {
            return TestPoints.Contains(testPoint);
        }
    }
}
