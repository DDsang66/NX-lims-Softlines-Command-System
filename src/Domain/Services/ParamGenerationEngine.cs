using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Conparison;

namespace NX_lims_Softlines_Command_System.src.Domain.Services
{
    /// <summary>
    /// 最简调度器：按 Priority 升序遍历规则，匹配则将结果写入 ParamSet。
    /// 若规则的 StopOnMatch 为 true，则匹配后立即停止调度（全局停止）。
    /// 若需要按参数名单独停止或替换策略，可在此扩展。
    /// </summary>
    public class ParamGenerationEngine:IParamGenerationEngine
    {
        private readonly IConditionAccessor _accessor;
        private readonly IValueComparer _comparer;

        public ParamGenerationEngine(IConditionAccessor accessor, IValueComparer comparer)
        {
            _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
            _comparer = comparer ?? throw new ArgumentNullException(nameof(comparer));
        }

        /// <summary>
        /// 根据条件池和规则集生成参数集
        /// </summary>
        /// <param name="pool"></param>
        /// <param name="rules"></param>
        /// <returns></returns>
        public ParamSet Generate(ConditionPool pool, IEnumerable<ParamRule> rules)
        {
            //按Priority升序排序
            var ruleCollection = rules?.OrderBy(r => r.Priority).ToList() ?? new List<ParamRule>();

            var result = new ParamSet();

            foreach (var rule in ruleCollection)
            {
                if (!rule.IsActive) continue;

                try
                {
                    if (rule.Match(pool, _accessor, _comparer))
                    {
                        var p = rule.GetResult();
                        result.Add(rule.ParamName, p?.Value);
                        if (rule.StopOnMatch)
                            break; // 简化策略：匹配到即停止
                    }
                }
                catch (KeyNotFoundException)
                {
                    // 条件池缺失时，跳过该规则（调用方应在生成前做完整性校验）
                    continue;
                }
                catch
                {
                    // 单条规则异常隔离：记录/跳过（日志可在 infra 注入）
                    continue;
                }
            }

            return result;
        }
    }
}
