using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using NX_lims_Softlines_Command_System.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Conparison;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext
{
    /// <summary>
    /// 参数规则聚合根
    /// 
    /// 职责：
    /// 1. 定义"何时"生成参数（通过 ConditionPattern 匹配条件池）
    /// 2. 匹配成功后，返回结果（固定值或从条件池动态取值）
    /// 
    /// 匹配模式（ConditionPattern 中可组合使用，顶层为 AND）：
    /// - EqualMatches：等值匹配（字段 == 期望值）
    /// - ComparisonMatches：比较匹配（字段 >=, <=, >, <, ==, != 期望值）
    /// - InMatches：集合匹配（字段 ∈ 允许集合）
    /// - CompositeMatches：复合条件（支持 AND/OR/NOT 嵌套）
    /// - AssignMatches：赋值匹配（从条件池取值，动态写入 Result）
    /// 
    /// 运行时结果与配置结果的分离：
    /// - Result：配置态，来自数据库，不可变
    /// - _runtimeResult：运行时态，Match 时动态生成，不持久化
    /// - GetResult()：优先返回运行时结果，其次返回配置结果
    /// </summary>
    public sealed class ParamRule : AggregateRoot<ParamRuleId, string>
    {
        #region 属性

        /// <summary>
        /// 所属公式
        /// </summary>
        public FormulaId? FormulaId { get; private set; }

        /// <summary>
        /// 所属参数结构
        /// </summary>
        public ParamStructureId? StructureId { get; private set; }

        /// <summary>
        /// 生成的参数名
        /// </summary>
        public string ParamName { get; private set; }

        /// <summary>
        /// 优先级（数字越小越高）
        /// </summary>
        public int Priority { get; private set; }

        /// <summary>
        /// 条件匹配模式
        /// </summary>
        public ConditionPattern Pattern { get; private set; }

        /// <summary>
        /// 配置的初始结果（来自数据库，不可变）
        /// 
        /// 说明：
        /// - 对于 Equal/Comparison/In/Composite 模式，这是匹配成功后的固定结果
        /// - 对于 AssignMatches 模式，此值仅作为占位，实际结果由运行时动态生成
        /// </summary>
        public ParamValue Result { get; private set; }

        /// <summary>
        /// 是否命中即停止
        /// </summary>
        public bool StopOnMatch { get; private set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; private set; }

        /// <summary>
        /// 规则所属引擎层级
        /// </summary>
        public EngineLayer EngineLayer { get; private set; }

        /// <summary>
        /// 运行时结果（Match 时动态生成，不持久化）
        /// 
        /// 设计意图：
        /// - 避免 Match 方法污染配置态的 Result
        /// - 同一规则实例可被多次 Match（不同条件池），每次重新生成运行时结果
        /// - 未 Match 或 Match 失败时，此值为 null，GetResult() 回退到配置态的 Result
        /// </summary>
        private ParamValue? _runtimeResult;

        #endregion

        #region 构造函数与工厂方法

        /// <summary>
        /// 私有构造函数（EF Core / 仓储使用）
        /// </summary>
        private ParamRule() { }

        /// <summary>
        /// 静态工厂方法：创建参数规则聚合根
        /// </summary>
        public static ParamRule Create(
            ParamRuleId id,
            FormulaId? formulaId,
            ParamStructureId? structureId,
            string paramName,
            int priority,
            EngineLayer engineLayer,
            ConditionPattern pattern,
            ParamValue? result = null,
            bool stopOnMatch = true,
            bool isActive = false)
        {
            // 集中进行业务规则校验和不变式保护
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (string.IsNullOrWhiteSpace(paramName))
                throw new ArgumentException("参数名不能为空", nameof(paramName));
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern));
            if (priority < 1)
                throw new ArgumentOutOfRangeException(nameof(priority), "优先级不能小于1");
            if (engineLayer == null)
                throw new ArgumentNullException(nameof(engineLayer));

            return new ParamRule
            {
                Id = id,
                FormulaId = formulaId,
                StructureId = structureId,
                ParamName = paramName,
                Priority = priority,
                Pattern = pattern,
                Result = result ?? new ParamValue(),
                StopOnMatch = stopOnMatch,
                IsActive = isActive,
                EngineLayer = engineLayer
            };
        }

        /// <summary>
        /// 从数据库重建 ParamRule（仓储层使用，不校验业务规则）
        /// </summary>
        internal static ParamRule Reconstitute(
            ParamRuleId id,
            FormulaId? formulaId,
            ParamStructureId? structureId,
            string paramName,
            int priority,
            ParamValue result,
            bool stopOnMatch,
            bool isActive,
            ConditionPattern pattern,
            EngineLayer engineLayer)
        {
            return new ParamRule
            {
                Id = id,
                FormulaId = formulaId,
                StructureId = structureId,
                ParamName = paramName,
                Priority = priority,
                Result = result,
                StopOnMatch = stopOnMatch,
                IsActive = isActive,
                Pattern = pattern,
                EngineLayer = engineLayer
            };
        }

        #endregion

        #region 行为方法

        /// <summary>
        /// 更新可变字段
        /// 
        /// 说明：
        /// - 更新后自动将 IsActive 置为 false（需重新 Active）
        /// - 如果当前处于激活状态，更新后必须仍满足激活条件
        /// </summary>
        public void Update(
            ConditionPattern pattern,
            ParamValue result,
            int priority,
            bool stopOnMatch)
        {
            // 1. 基础参数校验
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern), "条件匹配模式不能为空");

            if (priority < 1)
                throw new ArgumentOutOfRangeException(nameof(priority), "优先级不能小于1");

            if (result == null)
                throw new ArgumentNullException(nameof(result), "规则结果不能为空");

            // 2. 如果当前规则处于激活状态，更新后必须仍满足激活条件
            if (IsActive)
            {
                // 2.1 条件模式不能为空（至少有一种匹配规则，含 AssignMatches）
                if (!HasAnyMatchPattern(pattern))
                {
                    throw new InvalidOperationException("更新失败：激活状态下的规则，条件模式不能为空");
                }

                // 2.2 必须有有效的结果值
                //     注意：AssignMatches 模式下 Result 是运行时动态生成的，
                //     配置态允许为 null（占位），因此这里只对"非纯赋值模式"校验
                if (!IsPureAssignPattern(pattern) && (result.Value == null))
                {
                    throw new InvalidOperationException("更新失败：激活状态下的规则，结果值不能为空");
                }
            }

            // 3. 状态赋值
            this.Pattern = pattern;
            this.Result = result;
            this.Priority = priority;
            this.StopOnMatch = stopOnMatch;
            this.IsActive = false;

            // 4. 清除运行时结果（配置已变更，旧结果失效）
            this._runtimeResult = null;
        }

        /// <summary>
        /// 调整优先级
        /// </summary>
        public void ChangePriority(int newPriority)
        {
            if (newPriority < 1)
                throw new ArgumentOutOfRangeException(nameof(newPriority), "优先级不能小于1");

            this.Priority = newPriority;
            // 委托查询统一公式下的参数规则集检查是否有相同的优先级
        }

        /// <summary>
        /// 激活规则
        /// 
        /// 激活条件：
        /// 1. 必须关联公式
        /// 2. 必须关联参数结构
        /// 3. 必须有条件模式
        /// 4. 条件模式不能为空（至少有一种匹配规则，含 AssignMatches）
        /// 5. 必须有结果值（纯赋值模式除外，因为其结果运行时生成）
        /// 6. 必须属于某个层级
        /// </summary>
        public void Active()
        {
            // 1. 必须关联公式
            if (FormulaId == null)
                throw new InvalidOperationException("规则必须关联公式后才能激活");

            // 2. 必须关联参数结构
            if (StructureId == null)
                throw new InvalidOperationException("规则必须关联参数结构后才能激活");

            // 3. 必须有有效的条件模式
            if (Pattern == null)
                throw new InvalidOperationException("规则必须包含条件模式");

            // 4. 条件模式不能为空（至少有一种匹配规则，含 AssignMatches）
            if (!HasAnyMatchPattern(Pattern))
            {
                throw new InvalidOperationException("条件模式不能为空");
            }

            // 5. 必须有结果值（纯赋值模式可以豁免）
            if (!IsPureAssignPattern(Pattern))
            {
                if (Result == null || Result.Value == null)
                {
                    throw new InvalidOperationException("规则必须包含结果值");
                }
            }

            // 6. 必须属于某个层级
            if (EngineLayer == null)
            {
                throw new InvalidOperationException("规则必须属于某个层级");
            }

            IsActive = true;
        }

        /// <summary>
        /// 禁用规则
        /// </summary>
        public void Deactive()
        {
            IsActive = false;
            _runtimeResult = null;
        }

        #endregion

        #region 运行时匹配

        /// <summary>
        /// 运行时匹配
        /// 
        /// 执行流程：
        /// 1. 门禁检查：Equal / Comparison / In / Composite（任一失败立即返回 false）
        /// 2. 赋值匹配：AssignMatches（从条件池取值，写入运行时结果）
        /// 3. 非赋值模式：将配置的 Result 复制到运行时结果
        /// 
        /// 重要保证：
        /// - 匹配失败时，运行时结果不会被污染（先收集局部变量，全部成功后再写入）
        /// - 配置态的 Result 永远不会被修改
        /// </summary>
        /// <param name="pool">条件池</param>
        /// <param name="accessor">条件池访问器（负责路径解析、取值）</param>
        /// <param name="comparer">值比较器（负责等值/比较/truthy 判断）</param>
        /// <returns>是否匹配成功</returns>
        public bool Match(
            ConditionPool pool,
            IConditionPoolDomainService accessor,
            IValueComparer comparer)
        {
            if (!IsActive || Pattern == null || pool == null)
                return false;

            // 每次 Match 前清除旧的运行时结果，避免残留
            _runtimeResult = null;

            // ============================================================
            // 第一部分：门禁检查（所有条件必须通过）
            // ============================================================

            // 1.1 Equal 匹配
            foreach (var (field, expected) in Pattern.EqualMatches)
            {
                if (!accessor.TryGet(pool, field, out var actual))
                    return false;
                if (!comparer.AreEqual(actual, expected))
                    return false;
            }

            // 1.2 Comparison 匹配
            foreach (var comp in Pattern.ComparisonMatches)
            {
                if (!accessor.TryGet(pool, comp.FieldPath, out var actual))
                    return false;
                if (!comparer.Compare(actual, comp.Operator, comp.ExpectedValue))
                    return false;
            }

            // 1.3 In 匹配
            foreach (var (field, allowed) in Pattern.InMatches)
            {
                if (!accessor.TryGet(pool, field, out var actual))
                    return false;

                var ok = allowed?.Any(av => comparer.AreEqual(av, actual)) ?? false;
                if (!ok)
                    return false;
            }

            // 1.4 Composite 匹配（递归评估复合条件）
            foreach (var comp in Pattern.CompositeMatches)
            {
                if (!EvaluateComposite(comp, pool, accessor, comparer))
                    return false;
            }

            // ============================================================
            // 第二部分：生成运行时结果
            // ============================================================

            if (Pattern.AssignMatches.Any())
            {
                // ---- 赋值匹配模式：从条件池取值 ----
                var assignValues = new List<object?>();

                foreach (var assign in Pattern.AssignMatches)
                {
                    if (!accessor.TryGet(pool, assign.SourceFieldPath, out var sourceValue))
                    {
                        if (assign.IsRequired)
                        {
                            // 必填但取不到值 → 匹配失败（此时 _runtimeResult 仍为 null）
                            return false;
                        }

                        // 非必填 → 使用默认值
                        sourceValue = assign.DefaultValue;
                    }

                    assignValues.Add(sourceValue);
                }

                // 全部取值成功后再写入运行时结果
                var finalValue = assignValues.Count == 1
                    ? assignValues[0]
                    : assignValues;

                _runtimeResult = new ParamValue(finalValue);
            }
            else
            {
                // ---- 非赋值模式：使用配置的 Result ----
                _runtimeResult = Result;
            }

            return true;
        }

        /// <summary>
        /// 递归计算复合条件
        /// 
        /// 评估顺序：
        /// 1. FieldNames：优先匹配顶层 Pattern 的 Equal/In/Comparison，否则作为 truthy 检查
        /// 2. SubConditions：直接按比较条件评估
        /// 3. Children：递归评估子复合节点
        /// 
        /// 合并逻辑：
        /// - And：所有子项为 true（遇 false 短路）
        /// - Or：任一子项为 true（遇 true 短路）
        /// - Not：所有子项均为 false（即 !Any(true)）
        /// </summary>
        private bool EvaluateComposite(
            CompositeCondition composite,
            ConditionPool pool,
            IConditionPoolDomainService accessor,
            IValueComparer comparer)
        {
            if (composite == null)
                return true;

            var results = new List<bool>();

            // 1. FieldNames
            if (composite.FieldNames != null)
            {
                foreach (var fn in composite.FieldNames)
                {
                    if (!accessor.TryGet(pool, fn, out var val))
                    {
                        results.Add(false);
                        if (composite.Logic == LogicalOperator.And) return false;
                        continue;
                    }

                    // 1.1 优先匹配顶层 EqualMatches
                    if (Pattern.EqualMatches.ContainsKey(fn))
                    {
                        var r = comparer.AreEqual(val, Pattern.EqualMatches[fn]);
                        results.Add(r);
                        if (composite.Logic == LogicalOperator.And && !r) return false;
                        if (composite.Logic == LogicalOperator.Or && r) return true;
                        continue;
                    }

                    // 1.2 优先匹配顶层 InMatches
                    if (Pattern.InMatches.ContainsKey(fn))
                    {
                        var allowed = Pattern.InMatches[fn];
                        var matched = allowed != null && allowed.Any(av => comparer.AreEqual(av, val));
                        results.Add(matched);
                        if (composite.Logic == LogicalOperator.And && !matched) return false;
                        if (composite.Logic == LogicalOperator.Or && matched) return true;
                        continue;
                    }

                    // 1.3 优先匹配顶层 ComparisonMatches
                    var compMatch = Pattern.ComparisonMatches.FirstOrDefault(c =>
                        string.Equals(c.FieldPath, fn, StringComparison.OrdinalIgnoreCase));

                    if (compMatch != null)
                    {
                        var r = comparer.Compare(val, compMatch.Operator, compMatch.ExpectedValue);
                        results.Add(r);
                        if (composite.Logic == LogicalOperator.And && !r) return false;
                        if (composite.Logic == LogicalOperator.Or && r) return true;
                        continue;
                    }

                    // 1.4 无显式规则 → truthy 判定
                    var truthy = comparer.IsTruthy(val);
                    results.Add(truthy);
                    if (composite.Logic == LogicalOperator.And && !truthy) return false;
                    if (composite.Logic == LogicalOperator.Or && truthy) return true;
                }
            }

            // 2. SubConditions
            if (composite.SubConditions != null)
            {
                foreach (var sub in composite.SubConditions)
                {
                    if (!accessor.TryGet(pool, sub.FieldPath, out var actual))
                    {
                        results.Add(false);
                        if (composite.Logic == LogicalOperator.And) return false;
                        continue;
                    }

                    var r = comparer.Compare(actual, sub.Operator, sub.ExpectedValue);
                    results.Add(r);
                    if (composite.Logic == LogicalOperator.And && !r) return false;
                    if (composite.Logic == LogicalOperator.Or && r) return true;
                }
            }

            // 3. Children（递归）
            if (composite.Children != null)
            {
                foreach (var child in composite.Children)
                {
                    var childResult = EvaluateComposite(child, pool, accessor, comparer);
                    if (composite.Logic == LogicalOperator.And && !childResult) return false;
                    if (composite.Logic == LogicalOperator.Or && childResult) return true;
                    results.Add(childResult);
                }
            }

            // 4. 合并结果
            if (!results.Any())
                return false;

            return composite.Logic switch
            {
                LogicalOperator.And => results.All(x => x),
                LogicalOperator.Or => results.Any(x => x),
                LogicalOperator.Not => !results.Any(x => x),
                _ => results.All(x => x)
            };
        }

        #endregion

        #region 查询方法

        /// <summary>
        /// 获取当前规则的结果
        /// 
        /// 优先级：
        /// 1. 运行时结果（Match 成功后生成）
        /// 2. 配置结果（来自数据库）
        /// 
        /// 说明：
        /// - 当前结果需要通过复杂校验后，才能写入 CheckList 中的 ParamSet（最终结果）
        /// </summary>
        public ParamValue GetResult()
        {
            return _runtimeResult ?? Result;
        }

        /// <summary>
        /// 声明当前规则所含有的条件字段名集合
        /// </summary>
        public IEnumerable<string> RequiredConditions()
        {
            return Pattern.RequiredConditions();
        }

        #endregion

        #region 私有辅助方法

        /// <summary>
        /// 判断条件模式是否至少包含一种匹配规则
        /// </summary>
        private static bool HasAnyMatchPattern(ConditionPattern pattern)
        {
            return pattern.EqualMatches.Any()
                || pattern.ComparisonMatches.Any()
                || pattern.InMatches.Any()
                || pattern.CompositeMatches.Any()
                || pattern.AssignMatches.Any();
        }

        /// <summary>
        /// 判断是否为纯赋值模式
        /// 
        /// 纯赋值模式：只有 AssignMatches，没有其他门禁条件
        /// 此类模式的结果完全由运行时生成，配置态 Result 允许为 null
        /// </summary>
        private static bool IsPureAssignPattern(ConditionPattern pattern)
        {
            return pattern.AssignMatches.Any()
                && !pattern.EqualMatches.Any()
                && !pattern.ComparisonMatches.Any()
                && !pattern.InMatches.Any()
                && !pattern.CompositeMatches.Any();
        }

        #endregion
    }
}

