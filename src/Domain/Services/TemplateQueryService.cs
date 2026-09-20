using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;

namespace NX_lims_Softlines_Command_System.src.Domain.Services
{
    public class TemplateSelectService: ITemplateSelectService, IScopedDependency
    {
        private readonly ITemplateRepository _templateRepository;

        public TemplateSelectService(ITemplateRepository templateRepository)
        {
            _templateRepository = templateRepository ?? throw new ArgumentNullException(nameof(templateRepository));
        }

        public Template? FindByIndex(IReadOnlyDictionary<string, object?> conditions)
        {
            if (conditions == null || conditions.Count == 0)
                throw new ArgumentException("查询条件不能为空", nameof(conditions));

            // 1. 先按第一个条件做粗筛（仓储层尽量下推到数据库）
            //    这里假设仓储提供了按单个 key-value 查询的能力
            var firstCondition = conditions.First();
            var candidates = _templateRepository.FindByIndexKey(firstCondition.Key, firstCondition.Value);

            // 2. 在内存中做完整匹配：候选模板的索引必须包含所有查询条件
            var matched = candidates
                .Where(t => t.Status == Status.Active)   // 只查已发布的模板
                .Where(t => t.TemplateIndex != null)
                .Where(t => MatchesAll(t.TemplateIndex, conditions))
                .ToList();

            // 3. 处理结果
            if (matched.Count == 0)
                return null;

            if (matched.Count > 1)
                throw new InvalidOperationException(
                    $"索引条件命中了 {matched.Count} 个模板，无法唯一确定。条件：{FormatConditions(conditions)}");

            return matched[0];
        }

        public Template? FindByUrl(string templateUrl)
        {
            if (string.IsNullOrWhiteSpace(templateUrl))
                throw new ArgumentException("模板URL不能为空", nameof(templateUrl));

            return _templateRepository.FindByUrl(templateUrl);
        }

        /// <summary>
        /// 判断模板索引是否满足所有查询条件
        /// </summary>
        private static bool MatchesAll(TemplateIndex index, IReadOnlyDictionary<string, object?> conditions)
        {
            foreach (var kvp in conditions)
            {
                if (!index.Values.TryGetValue(kvp.Key, out var indexValue))
                    return false;

                if (!AreEqual(indexValue, kvp.Value))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 值比较：处理大小写、类型转换（如字符串 "4N" vs 数字 4）
        /// </summary>
        private static bool AreEqual(object? a, object? b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;

            // 字符串比较忽略大小写和首尾空格
            if (a is string sa && b is string sb)
                return string.Equals(sa.Trim(), sb.Trim(), StringComparison.OrdinalIgnoreCase);

            // 数字比较
            if (IsNumeric(a) && IsNumeric(b))
                return Convert.ToDecimal(a) == Convert.ToDecimal(b);

            // 其他类型直接比较
            return a.Equals(b);
        }

        private static bool IsNumeric(object value)
        {
            return value is byte or sbyte or short or ushort or int or uint
                or long or ulong or float or double or decimal;
        }

        private static string FormatConditions(IReadOnlyDictionary<string, object?> conditions)
        {
            return string.Join(", ", conditions.Select(kv => $"{kv.Key}={kv.Value}"));
        }
    }
}
