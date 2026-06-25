using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.Enums;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj
{
    public class ConditionPattern
    {
        /// <summary>
        /// 等值匹配：字段 -> 期望值
        /// </summary>
        public Dictionary<string, object?> EqualMatches { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 比较匹配集合（支持 >=, <=, >, <, ==, !=）
        /// </summary>
        public List<ComparisonCondition> ComparisonMatches { get; init; } = new();

        /// <summary>
        /// 集合匹配（字段的值属于 AllowedValues 即匹配）
        /// </summary>
        public Dictionary<string, List<object?>> InMatches { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 复合条件（可表达简单的 AND/OR/NOT 组合）
        /// 每个 CompositeCondition 引用上面声明的字段（FieldNames）或直接包含子条件（简单场景）
        /// </summary>
        public List<CompositeCondition> CompositeMatches { get; init; } = new();

        // 例如：{ "FiberDominantType": "Synthetic", "BuyerSpecified": false }
        public ConditionPattern() { }

        // 方便构造器
        public void AddEqual(string field, object? value)
        {
            if (string.IsNullOrWhiteSpace(field)) throw new ArgumentException(nameof(field));
            EqualMatches[field] = value;
        }

        public void AddComparison(string fieldPath, ComparisonOperator op, object? value)
        {
            if (string.IsNullOrWhiteSpace(fieldPath)) throw new ArgumentException(nameof(fieldPath));
            ComparisonMatches.Add(new ComparisonCondition(fieldPath, op, value));
        }

        public void AddIn(string field, IEnumerable<object?> values)
        {
            if (string.IsNullOrWhiteSpace(field)) throw new ArgumentException(nameof(field));
            InMatches[field] = values?.ToList() ?? new List<object?>();
        }

        public void AddComposite(CompositeCondition composite)
        {
            if (composite == null) throw new ArgumentNullException(nameof(composite));
            CompositeMatches.Add(composite);
        }

        /// <summary>
        /// 声明当前规则所需的条件字段名集合
        /// （用于前置完整性校验／编辑器显示／索引）
        /// </summary>
        public IEnumerable<string> RequiredConditions()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var k in EqualMatches.Keys) set.Add(k);
            foreach (var c in ComparisonMatches) if (!string.IsNullOrWhiteSpace(c.FieldPath)) set.Add(c.FieldPath);
            foreach (var k in InMatches.Keys) set.Add(k);

            // CompositeCondition 可能包含直接字段引用
            foreach (var comp in CompositeMatches)
            {
                if (comp.FieldNames != null)
                {
                    foreach (var f in comp.FieldNames.Where(fn => !string.IsNullOrWhiteSpace(fn)))
                        set.Add(f);
                }

                // 如果 composite 包含 nested comparisons, include them
                if (comp.SubConditions != null)
                {
                    foreach (var sc in comp.SubConditions)
                    {
                        if (!string.IsNullOrWhiteSpace(sc.FieldPath)) set.Add(sc.FieldPath);
                    }
                }
            }

            return set;
        }
    }
}
