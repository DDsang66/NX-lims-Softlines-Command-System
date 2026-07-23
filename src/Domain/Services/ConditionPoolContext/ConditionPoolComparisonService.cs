using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Services.ConditionPoolContext
{
    public class ConditionPoolComparisonService : IConditionPoolComparisonService,IScopedDependency
    {
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
