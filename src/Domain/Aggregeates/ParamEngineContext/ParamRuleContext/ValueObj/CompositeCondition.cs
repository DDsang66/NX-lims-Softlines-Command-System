using Microsoft.Extensions.FileSystemGlobbing.Internal;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.Enums;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj
{
    /// <summary>
    /// 复合条件：支持嵌套逻辑树，用于在单个复合节点内表达复杂布尔组合。
    /// 
    /// 主要使用说明（简要）：
    /// - 本类型用于在 ConditionPattern 的 `CompositeMatches` 中构建布尔表达式树，
    ///   每个节点同时可包含三类子项：`FieldNames`、`SubConditions`、`Children`。
    /// - 节点的 <see cref="Logic"/> 决定该节点如何合并其所有直接子项（子项的 bool 结果按短路规则组合）：
    ///   - And：所有子项必须为 true（遇到 false 即短路返回 false）。
    ///   - Or：任一子项为 true 则短路返回 true。
    ///   - Not：被定义为“子项中没有 true”（即对子项的 OR 取反，等价于 !Any(child == true)）。
    /// - 评估顺序与优先级：
    ///   1. 对于每个 FieldName，会先尝试在顶层 Pattern 的 EqualMatches/InMatches/ComparisonMatches 中找到对应规则并按其规则比较；
    ///      若找不到，则对该字段值使用 truthy 判定（由 IValueComparer.IsTruthy 提供）。
    ///   2. SubConditions 是显式的比较表达式（ComparisonCondition），直接取值并使用比较器比较。
    ///   3. Children 是递归的子复合节点，可构成任意深度的布尔树。
    /// - 顶层注意：
    ///   - ParamRule 在顶层会对 Pattern.CompositeMatches 的每个顶层项逐个 Evaluate（默认按顶层逻辑为 AND 组合），
    ///     若需自定义顶层组合，应将多个逻辑节点放到一个“根 Composite”的 Children 中，由根节点的 Logic 决定合并策略。
    /// 
    /// 示例（用嵌套节点表达 A => B，即 Not(A) OR B）：
    /// {
    ///   "CompositeMatches": [
    ///     {
    ///       "Logic": "Or",
    ///       "Children": [
    ///         { "Logic": "Not", "FieldNames": ["A"], "Children": [] },
    ///         { "Logic": "Or",  "FieldNames": ["B"], "Children": [] }
    ///       ]
    ///     }
    ///   ]
    /// }
    /// 
    /// 设计与实现建议（简要）：
    /// - 在编辑器中以树形 UI 构建 CompositeCondition，避免把多个顶层节点误认为可以直接用任意组合（应放在单根节点下）。
    /// - 明确 `Not` 的语义（当前为对子项 OR 的取反），若需要“先按 AND 合并后再取反”的语义，请统一并补充单元测试。
    /// - 对复杂业务逻辑（如 XOR、IMPLIES）建议通过嵌套组合构造表达，或在语法层面加入专用运算符以提升可读性。
    /// </summary>
    public class CompositeCondition
    {
        /// <summary>
        /// 该节点的合并逻辑：And / Or / Not。
        /// - And：所有直接子项（FieldNames/SubConditions/Children）均为 true 时节点为 true。
        /// - Or：任一直接子项为 true 时节点为 true。
        /// - Not：当且仅当所有直接子项均为 false 时节点为 true（即 !Any(child == true)）。
        /// </summary>
        public LogicalOperator Logic { get; set; } = LogicalOperator.And;

        /// <summary>
        /// 直接引用的字段名列表。每个字段的评估：
        /// 1. 若在顶层 Pattern 中有对应 Equal/In/Comparison 规则，则优先按该规则比较；
        /// 2. 否则对该字段值使用 truthy 判定（由 IValueComparer.IsTruthy 决定）。
        /// 字段名大小写不敏感。
        /// </summary>
        public List<string>? FieldNames { get; set; }

        /// <summary>
        /// 显式的比较子条件集合（ComparisonCondition），用于在节点内表达具体比较（>=, <=, >, <, ==, !=）。
        /// 每个 SubCondition 单独取值并比较，结果参与本节点的 Logic 合并。
        /// </summary>
        public List<ComparisonCondition>? SubConditions { get; set; }

        /// <summary>
        /// 递归子复合节点，允许任意深度嵌套以构建复杂布尔树。
        /// - 推荐做法：若需要自定义多个顶层 composite 的合并策略（如 Or 连接多个顶层节点），
        ///   在 Pattern.CompositeMatches 中仅放入一个根节点，并把原来的多个顶层节点作为该根的 Children。
        /// </summary>
        public List<CompositeCondition>? Children { get; set; }
    }
}
