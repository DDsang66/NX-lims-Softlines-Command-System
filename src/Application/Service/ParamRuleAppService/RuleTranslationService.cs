using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Interface;
using System.Text.Json;

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
        private readonly IParser _parser;

        public RuleTranslationService(
            IConditionPatternBuilder patternBuilder,
            ITokenizer tokenizer,
            IParser parser
            )
        {
            _patternBuilder = patternBuilder;
            _tokenizer = tokenizer;
            _parser = parser;
        }

        /// <summary>
        /// 根据DTO创建条件模式
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public ConditionPattern PatternTranslateFromDto(CreateParamRuleRequest request,CancellationToken ct)
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
        /// 根据自然语言文本创建条件模式
        /// </summary>
        /// <param name="text"></param>
        /// <param name="formula"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public (ConditionPattern pattern,ParamValue paramValue) ParseFromNaturalLanguageText(string text, Formula formula, CancellationToken ct)
        {
            // 处理文本到领域对象的转换
            var tokens = _tokenizer.Tokenize(text);

            var parsedRule = _parser.Parse(tokens, formula);

            // JSON 反序列化为 ConditionPattern
            var pattern = parsedRule.ConditionPatternJson.Deserialize<ConditionPattern>();

            var paramValue = new ParamValue(parsedRule.ResultValue);

            return (pattern, paramValue);
        }

        /// <summary>
        /// 根据JSON创建条件模式
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
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

        /// <summary>
        /// 根据JSON创建逻辑运算符
        /// </summary>
        /// <param name="logic"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
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
