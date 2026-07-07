using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Interface;

namespace NX_lims_Softlines_Command_System.src.Application.Service.ParamRuleAppService
{
    /// <summary>
    /// 语言规则翻译服务
    /// 支持文本结构、json结构规则
    /// </summary>
    public class RuleTranslationService: IRuleTranslationService,IScopedDependency
    {
        private readonly IConditionPatternBuilder _patternBuilder;
        private readonly ITokenizer _tokenizer;
        private readonly IRuleParser _ruleParser;

        public RuleTranslationService(
            IConditionPatternBuilder patternBuilder,
            ITokenizer tokenizer,
            IRuleParser ruleParser)
        {
            _patternBuilder = patternBuilder;
            _tokenizer = tokenizer;
            _ruleParser = ruleParser;
        }

        /// <summary>
        /// 根据DTO创建条件模式
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public ConditionPattern TranslateFromDto(CreateParamRuleRequest request,CancellationToken ct)
        {
            // 处理DTO到领域对象的转换
            foreach (var match in request.EqualMatches)
            {
                _patternBuilder.AddEqual(match.Field, match.Value);
            }

            foreach (var match in request.ComparisonMatches)
            {
                _patternBuilder.AddComparison(
                    match.FieldPath,
                    ParseComparisonOperator(match.Operator),
                    match.ExpectedValue);
            }

            foreach (var match in request.ComparisonMatches)
            {
                _patternBuilder.AddComparison(
                    match.FieldPath,
                    ParseComparisonOperator(match.Operator),
                    match.ExpectedValue);
            }

            foreach (var match in request.InMatches)
            {
                _patternBuilder.AddIn(match.Field, match.Values);
            }

            foreach (var match in request.CompositeMatches)
            {
                var composite = new CompositeCondition
                {
                    Logic = ParseLogicalOperator(match.Logic),
                    FieldNames = match.FieldNames,
                    SubConditions = match.SubConditions.Select(sc => new ComparisonCondition
                    {
                        FieldPath = sc.FieldPath,
                        Operator = ParseComparisonOperator(sc.Operator),
                        ExpectedValue = sc.ExpectedValue
                    }).ToList()
                };
                _patternBuilder.AddComposite(composite);
            }

            return _patternBuilder.Build();
        }

        /// <summary>
        /// 根据文本创建条件模式
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public ConditionPattern ParseFromText(string text, CancellationToken ct)
        {
            // 处理文本到领域对象的转换
            var tokens = _tokenizer.Tokenize(text);

            return _ruleParser.Parse(tokens);
        }

        private ComparisonOperator ParseComparisonOperator(string op)
        {
            return op switch
            {
                "Equal" => ComparisonOperator.Equal,
                "NotEqual" => ComparisonOperator.NotEqual,
                "GreaterThan" => ComparisonOperator.GreaterThan,
                "GreaterThanOrEqual" => ComparisonOperator.GreaterThanOrEqual,
                "LessThan" => ComparisonOperator.LessThan,
                "LessThanOrEqual" => ComparisonOperator.LessThanOrEqual,
                _ => throw new ArgumentException($"Unknown operator: {op}")
            };
        }
        private LogicalOperator ParseLogicalOperator(string logic)
        {
            return logic switch
            {
                "And" => LogicalOperator.And,
                "Or" => LogicalOperator.Or,
                "Not" => LogicalOperator.Not,
                _ => throw new ArgumentException($"Unknown logic: {logic}")
            };
        }
    }
}
