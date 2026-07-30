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
using System.Globalization;
using System.Text.RegularExpressions;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext
{
    public sealed class ParamRule : AggregateRoot<ParamRuleId, string>
    {
        /// <summary>
        /// 参数规则id
        /// </summary>
        //public ParamRuleId Id { get; private set; }

        /// <summary>
        /// 所属公式
        /// </summary>
        public FormulaId? FormulaId { get; private set; } // 所属公式

        /// <summary>
        /// 所属结构
        /// </summary>
        public ParamStructureId? StructureId { get; private set; } // 所属结构

        /// <summary>
        /// 所属条件池
        /// </summary>
        public string ParamName { get; private set; } // 生成的参数名

        /// <summary>
        /// 优先级
        /// </summary>
        public int Priority { get; private set; }  // 优先级（数字越小越高）

        /// <summary>
        /// 条件匹配模式
        /// </summary>
        public ConditionPattern Pattern { get; private set; } // 条件匹配模式

        /// <summary>
        /// 规则匹配后的初始结果
        /// </summary>
        public ParamValue Result { get; private set; } // 规则匹配后的初始结果，无副作用

        /// <summary>
        /// 命中停止
        /// </summary>
        public bool StopOnMatch { get; private set; } // 是否命中即停止

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; private set; }

        // 私有构造函数
        //private ParamRule() { }

        /// <summary>
        /// 静态工厂方法：创建参数规则聚合根
        /// </summary>
        public static ParamRule Create(
            ParamRuleId id,
            FormulaId? formulaId,
            ParamStructureId? structureId,
            string paramName,
            int priority,
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

            // 在工厂内部处理默认值逻辑，保持私有构造函数的纯粹性
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
                IsActive = isActive
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
            ConditionPattern pattern)
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
                Pattern = pattern
            };
        }

        /// <summary>
        /// 可变字段更新
        /// </summary>
        /// <param name="Pattern"></param>
        /// <param name="Result"></param>
        /// <param name="Priority"></param>
        /// <param name="StopOnMatch"></param>
        public void Update(
            ConditionPattern pattern, 
            ParamValue result , 
            int priority, 
            bool stopOnMatch) 
        {
            // 1. 基础参数校验（与 Create 和 ChangePriority 保持一致）
            if (pattern == null)
                throw new ArgumentNullException(nameof(pattern), "条件匹配模式不能为空");

            if (priority < 1)
                throw new ArgumentOutOfRangeException(nameof(priority), "优先级不能小于1");

            if (result == null)
                throw new ArgumentNullException(nameof(result), "规则结果不能为空");

            // 2. 如果当前规则处于激活状态，更新核心属性后必须确保仍然满足激活条件
            if (IsActive)
            {
                // 条件模式不能为空（至少有一种匹配规则）
                if (!pattern.EqualMatches.Any()
                    && !pattern.ComparisonMatches.Any()
                    && !pattern.InMatches.Any()
                    && !pattern.CompositeMatches.Any())
                {
                    throw new InvalidOperationException("更新失败：激活状态下的规则，条件模式不能为空");
                }

                // 必须有有效的结果值
                if (result.Value == null)
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
        }


        /*---------------------------------------------------------最简匹配计算--------------------------------------------------------------*/
        /// <summary>
        /// 是运行时行为：读取 ConditionPool 的值（支持路径访问、比较运算、范围判断等
        /// 支持Pattern中的多种匹配规则
        /// 运行时匹配：将具体的取值与比较职责委托给注入的技术组件（accessor/comparer）
        /// 注意：方法不再直接访问 ConditionPool 内部结构，聚合保持声明性
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="accessor"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        public bool Match(
            ConditionPool pool,
            IConditionPoolDomainService accessor,
            IValueComparer comparer)
        {
            if (!IsActive || Pattern == null || pool == null) return false;

            // Equal
            foreach (var (field, expected) in Pattern.EqualMatches)
            {
                if (!accessor.TryGet(pool, field, out var actual)) return false;
                if (!comparer.AreEqual(actual, expected)) return false;
            }

            // Comparison
            foreach (var comp in Pattern.ComparisonMatches)
            {
                if (!accessor.TryGet(pool, comp.FieldPath, out var actual)) return false;
                if (!comparer.Compare(actual, comp.Operator, comp.ExpectedValue)) return false;
            }

            // In
            foreach (var (field, allowed) in Pattern.InMatches)
            {
                if (!accessor.TryGet(pool, field, out var actual)) return false;
                var ok = allowed?.Any(av => comparer.AreEqual(av, actual)) ?? false;
                if (!ok) return false;
            }

            // Composite: delegate truthy/compare checks to comparer + accessor
            foreach (var comp in Pattern.CompositeMatches)
            {
                // evaluate composite using accessor/comparer locally
                bool compositeResult = EvaluateComposite(comp, pool, accessor, comparer);
                if (!compositeResult) return false;
            }

            return true;
        }

        /// <summary>
        /// EvaluateComposite: 递归计算复合条件
        /// </summary>
        /// <param name="composite"></param>
        /// <param name="pool"></param>
        /// <param name="accessor"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        private bool EvaluateComposite(
            CompositeCondition composite,
            ConditionPool pool,
            IConditionPoolDomainService accessor,
            IValueComparer comparer)
        {
            if (composite == null) return true;

            var results = new List<bool>();

            //1. FieldNames：按声明优先匹配 Pattern 的 Equal/In/Comparison；否则作为 truthy 检查
            if (composite.FieldNames != null)
            {
                foreach (var fn in composite.FieldNames)
                {
                    if (!accessor.TryGet(pool, fn, out var val)) { results.Add(false); if (composite.Logic == LogicalOperator.And) return false; continue; }
                    if (Pattern.EqualMatches.ContainsKey(fn))
                    {
                        results.Add(comparer.AreEqual(val, Pattern.EqualMatches[fn]));
                        if (composite.Logic == LogicalOperator.And && !results.Last()) return false;
                        if (composite.Logic == LogicalOperator.Or && results.Last()) return true;
                        continue;
                    }
                    if (Pattern.InMatches.ContainsKey(fn))
                    {
                        var allowed = Pattern.InMatches[fn];
                        var matched = allowed != null && allowed.Any(av => comparer.AreEqual(av, val));
                        results.Add(matched);
                        if (composite.Logic == LogicalOperator.And && !results.Last()) return false;
                        if (composite.Logic == LogicalOperator.Or && results.Last()) return true;
                        continue;
                    }
                    var compMatch = Pattern.ComparisonMatches.FirstOrDefault(c => string.Equals(c.FieldPath, fn, StringComparison.OrdinalIgnoreCase));
                    if (compMatch != null)
                    {
                        var rr = comparer.Compare(val, compMatch.Operator, compMatch.ExpectedValue);
                        results.Add(rr);
                        if (composite.Logic == LogicalOperator.And && !rr) return false;
                        if (composite.Logic == LogicalOperator.Or && rr) return true;
                        continue;
                    }
                    results.Add(comparer.IsTruthy(val));
                    if (composite.Logic == LogicalOperator.And && !results.Last()) return false;
                    if (composite.Logic == LogicalOperator.Or && results.Last()) return true;
                }
            }

            //2. SubConditions: 直接按比较条件评估
            if (composite.SubConditions != null)
            {
                foreach (var sub in composite.SubConditions)
                {
                    if (!accessor.TryGet(pool, sub.FieldPath, out var actual)) { results.Add(false); if (composite.Logic == LogicalOperator.And) return false; continue; }
                    var r = comparer.Compare(actual, sub.Operator, sub.ExpectedValue);
                    results.Add(r);
                    if (composite.Logic == LogicalOperator.And && !r) return false;
                    if (composite.Logic == LogicalOperator.Or && r) return true;
                }
            }

            // 3. Children（递归评估子复合节点）
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

            // 4. 合并结果，支持 Not 对整体取反
            bool combined;
            if (!results.Any()) combined = false;
            else combined = composite.Logic switch
            {
                LogicalOperator.And => results.All(x => x),
                LogicalOperator.Or => results.Any(x => x),
                LogicalOperator.Not => !results.Any(x => x),
                _ => results.All(x => x)
            };

            //if (composite.Logic == LogicalOperator.Not) combined = !combined;

            return combined;
        }

        ///*---------------------------------------------------------end--------------------------------------------------------------*/

        /// <summary>
        /// 暴露当前规则初始结果
        /// 当前结果需要通过复杂校验后才能写入CheckList中的ParamSet(最终结果)供后续使用
        /// </summary>
        /// <returns></returns>
        public ParamValue GetResult() => Result;

        /// <summary>
        /// 声明当前规则所含有的的条件字段名集合（仅包含 EqualMatches 的字段名）
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> RequiredConditions()
        {
            var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var k in Pattern.EqualMatches.Keys) fields.Add(k);
            foreach (var c in Pattern.ComparisonMatches) fields.Add(c.FieldPath);
            foreach (var k in Pattern.InMatches.Keys) fields.Add(k);
            foreach (var comp in Pattern.CompositeMatches)
            {
                if (comp.FieldNames != null)
                    foreach (var f in comp.FieldNames) fields.Add(f);
                if (comp.SubConditions != null)
                    foreach (var s in comp.SubConditions) fields.Add(s.FieldPath);
            }

            return fields;
        }

        /// <summary>
        /// 调整优先级
        /// </summary>
        /// <param name="newPriority"></param>
        /// <exception cref="Exception"></exception>
        public void ChangePriority(int newPriority)
        {
            if (newPriority < 1) throw new Exception("Invalid priority");
            this.Priority = newPriority;
            //委托查询统一公式下的参数规则集检查是否有相同的优先级
        }

        /// <summary>
        /// 激活规则
        /// </summary>
        public void Active()
        {
            // 1. 必须关联公式
            if (FormulaId == null)
                throw new InvalidOperationException("规则必须关联公式后才能激活");

            // 2. 必须关联参数结构（可选，根据业务决定）
            if (StructureId == null)
                throw new InvalidOperationException("规则必须关联参数结构后才能激活");

            // 3. 必须有有效的条件模式
            if (Pattern == null)
                throw new InvalidOperationException("规则必须包含条件模式");

            // 4. 条件模式不能为空（至少有一种匹配规则）
            if (!Pattern.EqualMatches.Any()
                && !Pattern.ComparisonMatches.Any()
                && !Pattern.InMatches.Any()
                && !Pattern.CompositeMatches.Any())
            {
                throw new InvalidOperationException("条件模式不能为空");
            }

            // 5. 必须有结果值
            if (Result == null || Result.Value == null)
                throw new InvalidOperationException("规则必须包含结果值");

            IsActive = true;
        }

        /// <summary>
        /// 禁用规则
        /// </summary>
        public void Deactive() => IsActive = false;
    }
}
