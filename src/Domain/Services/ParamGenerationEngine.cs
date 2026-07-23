using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Conparison;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using System.Diagnostics.CodeAnalysis;

namespace NX_lims_Softlines_Command_System.src.Domain.Services
{
    /// <summary>
    /// 最简调度器：按 Priority 升序遍历规则，匹配则将结果写入 ParamSet。
    /// 若规则的 StopOnMatch 为 true，则匹配后立即停止调度（全局停止）。
    /// 若需要按参数名单独停止或替换策略，可在此扩展。
    /// </summary>
    public class ParamGenerationEngine:IParamGenerationEngine,IScopedDependency
    {
        private readonly IConditionPoolDomainService _conditionAccessor;
        private readonly IValueComparer _valueComparer;
        private readonly ILogger<ParamGenerationEngine> _logger;

        public ParamGenerationEngine(
            IConditionPoolDomainService conditionAccessor,
            IValueComparer valueComparer,
            ILogger<ParamGenerationEngine> logger)
        {
            _conditionAccessor = conditionAccessor;
            _valueComparer = valueComparer;
            _logger = logger;
        }

        /// <summary>
        /// 执行规则集，生成参数集
        /// </summary>
        /// <param name="conditionPool">条件池，用于规则匹配</param>
        /// <param name="rules">待执行的规则集</param>
        /// <returns>包含所有匹配结果的参数集</returns>
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", 
            Justification = "ArgumentNullException will be thrown by constructor")]
        public ParamSet Generate(ConditionPool conditionPool, IEnumerable<ParamRule> rules)
        {
            var ruleCollection = rules?.OrderBy(r => r.Priority).ToList() ?? new List<ParamRule>();
            var result = new ParamSet();

            foreach (var rule in ruleCollection)
            {
                if (!rule.IsActive)
                {
                    _logger.LogDebug("Rule '{RuleName}' is not active, skipping.", rule.ParamName);
                    continue;
                }

                try
                {
                    if (rule.Match(conditionPool, _conditionAccessor, _valueComparer))
                    {
                        var ruleResult = rule.GetResult();
                        var resultValue = ruleResult?.Value;

                        // 写入结果
                        result.SetValueOrFallback(rule.ParamName, resultValue,null);
                        _logger.LogInformation("Rule '{RuleName}' matched and added value '{Value}' to result.", rule.ParamName, resultValue);

                        // 尊重规则自身的 StopOnMatch 配置
                        if (rule.StopOnMatch)
                        {
                            _logger.LogInformation("Rule '{RuleName}' has StopOnMatch enabled, stopping execution.", rule.ParamName);
                            break;
                        }
                    }
                }
                catch (KeyNotFoundException ex)
                {
                    _logger.LogWarning(ex, "Rule '{RuleName}' failed to execute due to missing condition in pool. Skipping.", rule.ParamName);
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Rule '{RuleName}' encountered an unexpected error. Skipping.", rule.ParamName);
                    continue;
                }
            }

            return result;
        }
    }
}
