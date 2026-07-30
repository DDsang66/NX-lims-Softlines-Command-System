using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.Enums;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj
{
    public class ConditionPattern
    {
        /// <summary>
        /// 等值匹配：字段 -> 期望值（key之间的关系是逻辑,即所有key的值满足才能匹配成功）
        /// </summary>
        public Dictionary<string, object?> EqualMatches { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 比较匹配集合（支持 >=, <=, >, <, ==, !=）
        /// 各个ComparisonCondition之间的关系是逻辑与，即所有条件都满足才能匹配成功
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



        /* =====================* 示例概述（场景）===========================================
        •	Pattern（示意）：
        •	EqualMatches:
                    •	FiberDominantType = "Synthetic"
        •	ComparisonMatches:
                    •	FiberContent.Polyester >= 50
        •	CompositeMatches:
                   1.	comp1：Logic = Or
                            •	FieldNames = ["IsSample", "Buyer"]
                            •	SubConditions = [ Weight < 100 ]
                    2.	comp2：Logic = Not
                            •	FieldNames = ["IsDefective"]
        •	ConditionPool（运行时实际值）：
                     •	FiberDominantType = "Synthetic"
                     •	FiberContent.Polyester = 55
                     •	Weight = 90
                     •	Buyer = "C"
                     •	IsSample = false
                     •	IsDefective = false  
        
        1.	前置检查
        •	ParamRule.Match(...) 首先检查 IsActive 与 Pattern != null，通过后继续。
        2.	EqualMatches（顶层是 AND）
        •	遍历 EqualMatches：
        •	调用 accessor.TryGet(pool, "FiberDominantType", out actual) -> 返回 "Synthetic"。
        •	comparer.AreEqual("Synthetic", "Synthetic") -> 返回 true。
        •	任何一项 false 会立即 return false（短路）。本例通过。
        3.	ComparisonMatches（顶层是 AND）
        •	遍历比较条件：
        •	accessor.TryGet(pool, "FiberContent.Polyester", out actual) -> 返回 55（TryGet 负责路径解析）。
        •	comparer.Compare(55, GreaterThanOrEqual, 50)：
        •	ValueComparer 会把两边尝试转换为 decimal -> 比较 55 >= 50 -> true。
        •	如果有多条比较，任一 false 即短路返回 false。本例通过。
        4.	InMatches（若存在，顶层也是 AND）
        •	本例没有顶层 InMatches，跳过。注意：若把 Buyer 放到顶层 InMatches，且 pool 中 Buyer="C" 不在允许列表中，则会在这里直接失败，不会走 composite。
        5.	CompositeMatches（顶层多个 composite 之间是 AND,即comp1与comp2是 "与" 关系）
        •	ParamRule.Match 对 Pattern.CompositeMatches 逐个 EvaluateComposite，任一 composite 返回 false 则整体失败（顶层 AND）。
        评估 comp1（Logic = Or，FieldNames = ["IsSample","Buyer"], SubConditions = [Weight < 100]）：
        •	FieldName "IsSample"：
        •	accessor.TryGet(pool,"IsSample", out val) -> false
        •	在顶层 pattern 中没有 EqualMatches/InMatches/ComparisonMatches 对应 "IsSample"，因此使用 comparer.IsTruthy(false) -> false。
        •	因为 Logic == Or，遇到 false 不短路，继续下一个字段。
        •	FieldName "Buyer"：
        •	accessor.TryGet(pool,"Buyer", out val) -> "C"
        •	同样顶层没有 Buyer 的显式规则，所以使用 comparer.IsTruthy("C")：
        •	ValueComparer.IsTruthy("C") -> true（非空字符串且不等于 "false"）。
        •	因为 Logic == Or 且得到 true，按短路规则立即返回 true（comp1 通过），不会再评估 SubConditions（实现中的短路会先对 FieldNames 和 SubConditions 各自短路；在当前实现，SubConditions 若还没评估也可以被跳过）。
        •	注意：如果 FieldNames 都为 false，则接着评估 SubConditions，直到找到 true 或全部为 false。
        •	结果：comp1 返回 true（短路发生于 Buyer 为 truthy）。
        评估 comp2（Logic = Not，FieldNames = ["IsDefective"]）：
        •	FieldName "IsDefective"：
        •	accessor.TryGet(pool,"IsDefective", out val) -> false
        •	没有顶层显式规则，使用 comparer.IsTruthy(false) -> false。
        •	results 列表包含单个 false。
        •	合并逻辑（已修复的 Not 语义）：
        •	合并时对 Not 的定义为 “none of the children is true” → 即 !results.Any(x => x)。
        •	因为 results 中没有 true（只有 false），results.Any(x=>x) 为 false，!false -> true。
        •	结果：comp2 返回 true。        
        6.最终结果
        •	顶层：EqualMatches true 
                       AND ComparisonMatches true 
                       AND CompositeMatches (comp1 true AND comp2 true) 
            => 整体 Match 返回 true。
        •	若任一顶层步骤失败（例如 EqualMatches 其中一项不等、或 ComparisonMatches 有一项比较不通过、或任一 composite 返回 false），则整体返回 false（短路尽早返回）。
        
        ========================= End =============================================================================*/
    }
}
