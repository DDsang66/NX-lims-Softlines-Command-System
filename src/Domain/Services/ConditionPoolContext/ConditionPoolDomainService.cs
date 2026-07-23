using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Services.ConditionPoolContext
{
    public class ConditionPoolDomainService : IConditionPoolDomainService, IScopedDependency
    {
        /// <summary>
        /// 条件访问器
        /// </summary>
        public bool TryGet(ConditionPool pool, string path, out object? value)
        {
            value = null;
            if (pool == null || string.IsNullOrWhiteSpace(path)) return false;

            // 直接键
            if (pool.Conditions.TryGetValue(path, out var direct))
            {
                value = direct;
                return true;
            }

            // 嵌套路径 A.B.C
            var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return false;

            if (!pool.Conditions.TryGetValue(parts[0], out var current)) return false;
            if (parts.Length == 1) { value = current; return true; }

            var dict = current as IDictionary<string, object?>;
            for (int i = 1; i < parts.Length; i++)
            {
                if (dict == null) return false;
                var key = parts[i];
                if (!dict.TryGetValue(key, out current)) return false;
                if (i == parts.Length - 1) { value = current; return true; }
                dict = current as IDictionary<string, object?>;
            }

            return false;
        }

        /// <summary>
        /// 条件池富化
        /// </summary>
        /// <param name="rawData"></param>
        /// <returns></returns>
        public ConditionPool Enrich(IDictionary<string, object?> rawData)
        {
            return null;
        }


        /// <summary>
        /// 根据参数结构生成所需的条件字典
        /// 对应1级条件池
        /// </summary>
        /// <param name="paramStructures"></param>
        /// <returns></returns>
        public IDictionary<string, object?> GenerateRequiredConditions(IEnumerable<ParamStructure> paramStructures)
        {
            var condition = new Dictionary<string, object?>();

            foreach (var paramStructure in paramStructures)
            {
                foreach (var requirement in paramStructure.Schema.ConditionRequirements)
                {
                    // 如果字段已存在，可以选择保留第一个或合并信息
                    if (!condition.ContainsKey(requirement.FieldName))
                    {
                        condition[requirement.FieldName] = new
                        {
                            Type = requirement.FieldName.GetType(),
                            requirement.IsRequired,
                            requirement.AllowedValues
                        };
                    }
                    else
                    {
                        var existing = (dynamic)condition[requirement.FieldName];
                        // 合并AllowedValues
                        var mergedValues = existing.AllowedValues
                            .Concat(requirement.AllowedValues)
                            .Distinct()
                            .ToList();

                        condition[requirement.FieldName] = new
                        {
                            Type = requirement.FieldName.GetType(),
                            IsRequired = requirement.IsRequired || existing.IsRequired,
                            AllowedValues = mergedValues
                        };
                    }
                }
            }

            return condition;
        }

        /// <summary>
        /// 根据条件分组
        /// </summary>
        /// <param name="originalPool"></param>
        /// <param name="groupData"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public (ConditionPool updatedPool, List<ConditionPool> poolsToUpdate, List<ConditionPool> poolsToCreate, List<ConditionPool> poolsToDelete) GroupWithReuse(
            ConditionPool originalPool,
            List<ConditionPool> existingPools,  // 传入已有池子
            List<(Dictionary<string, object?> Conditions, List<string> TestPoints)> groupData)
        {
            if (groupData.Count == 0) throw new ArgumentException();

            var poolsToUpdate = new List<ConditionPool>();
            var poolsToCreate = new List<ConditionPool>();

            // 第一个：原始池
            var firstItem = groupData.First();
            originalPool.MergeFrom(firstItem.Conditions, firstItem.TestPoints);
            poolsToUpdate.Add(originalPool);

            // 后续：优先复用已有，不够再新建
            var availablePools = existingPools
                .Where(p => p.Id != originalPool.Id)
                .OrderBy(p => p.CreatedAt)
                .ToList();

            for (int i = 1; i < groupData.Count; i++)
            {
                var data = groupData[i];

                if (i - 1 < availablePools.Count)
                {
                    // 复用已有
                    var pool = availablePools[i - 1];
                    pool.MergeFrom(data.Conditions, data.TestPoints);
                    poolsToUpdate.Add(pool);
                }
                else
                {
                    // 新建
                    var newPool = ConditionPool.Create(originalPool.CheckListId, data.Conditions);
                    newPool.AddTestPoints(data.TestPoints);
                    poolsToCreate.Add(newPool);
                }
            }

            // 返回多余的（需要删除）
            var poolsToDelete = availablePools.Skip(groupData.Count - 1).ToList();

            return (originalPool, poolsToUpdate, poolsToCreate, poolsToDelete);
        }

        /// <summary>
        /// 比较两个条件值是否相等
        /// </summary>
        /// <param name="left">条件字段1</param>
        /// <param name="right">条件字段2</param>
        /// <returns></returns>
        public bool AreConditionsEqual(ConditionPool left, ConditionPool right)
        {
            //如果两个条件池为空，认为他们都相等
            if (left == null || right == null) return left == right;

            var leftConditions = left.Conditions;
            var rightConditions = right.Conditions;

            //key个数比较
            if (leftConditions.Count != rightConditions.Count) return false;

            //value 等值比较
            foreach (var (key, leftValue) in leftConditions)
            {
                if (!rightConditions.TryGetValue(key, out var rightValue))
                    return false;

                if (!ValuesEqual(leftValue, rightValue))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 比较两个条件池是否兼容
        /// </summary>
        /// <param name="left">条件池1</param>
        /// <param name="right">条件池2</param>
        /// <param name="ignoredFields">可忽略字段</param>
        /// <returns></returns>
        public bool AreConditionsCompatible(
            ConditionPool left,
            ConditionPool right,
            IEnumerable<string>? ignoredFields = null)
        {
            // 步骤 1: 构建忽略字段的 HashSet
            // 使用 OrdinalIgnoreCase 确保大小写不敏感比较
            var ignoreSet = ignoredFields?.ToHashSet(StringComparer.OrdinalIgnoreCase)
                ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 步骤 2: 过滤掉需要忽略的字段
            // Where 筛选出不在 ignoreSet 中的键值对
            var leftDict = left.Conditions.Where(kv => !ignoreSet.Contains(kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            var rightDict = right.Conditions.Where(kv => !ignoreSet.Contains(kv.Key))
                .ToDictionary(kv => kv.Key, kv => kv.Value);

            if (leftDict.Count != rightDict.Count) return false;

            // 步骤 3: 比较过滤后的字典
            foreach (var (key, leftValue) in leftDict)
            {
                if (!rightDict.TryGetValue(key, out var rightValue)) return false;
                if (!ValuesEqual(leftValue, rightValue)) return false;
            }

            return true;
        }

        /// <summary>
        /// 比较两个条件池的差异
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public ConditionDiff Compare(ConditionPool left, ConditionPool right)
        {
            var diff = new ConditionDiff();

            var allKeys = left.Conditions.Keys
                .Union(right.Conditions.Keys, StringComparer.OrdinalIgnoreCase);

            foreach (var key in allKeys)
            {
                var leftHas = left.Conditions.TryGetValue(key, out var leftVal);
                var rightHas = right.Conditions.TryGetValue(key, out var rightVal);

                if (!leftHas) diff.Added[key] = rightVal!;
                else if (!rightHas) diff.Removed[key] = leftVal!;
                else if (!ValuesEqual(leftVal, rightVal)) diff.Modified[key] = (leftVal!, rightVal!);
            }

            //输出差异类
            return diff;
        }

        /// <summary>
        /// 比较两个值是否相等
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static bool ValuesEqual(object? a, object? b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            // 处理数值类型比较（如 int vs long）
            if (IsNumeric(a) && IsNumeric(b))
                return Convert.ToDecimal(a) == Convert.ToDecimal(b);

            return a.Equals(b);
        }

        /// <summary>
        /// 判断一个值是否为数值类型
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool IsNumeric(object value) => value is sbyte or byte or short or ushort
            or int or uint or long or ulong or float or double or decimal;
    }
}
