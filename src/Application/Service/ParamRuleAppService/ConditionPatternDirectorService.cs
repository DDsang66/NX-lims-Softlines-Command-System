using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.ParamRuleAppService
{
    public interface IConditionPatternDirectorService:IScopedDependency
    {
        ConditionPattern CreatePatternFromDto(CreateParamRuleRequest request);
    }

    public class ConditionPatternDirectorService : IConditionPatternDirectorService,IScopedDependency
    {
        private readonly IConditionPatternBuilder _builder;

        public ConditionPatternDirectorService(IConditionPatternBuilder builder)
        {
            _builder = builder;
        }

        /// <summary>
        /// 构建ConditionPattern
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public ConditionPattern CreatePatternFromDto(CreateParamRuleRequest request)
        {
            // 使用Builder构建ConditionPattern
            foreach (var match in request.EqualMatches)
            {
                _builder.AddEqual(match.Field, match.Value);
            }

            foreach (var match in request.ComparisonMatches)
            {
                _builder.AddComparison(
                    match.FieldPath,
                    ParseComparisonOperator(match.Operator),
                    match.ExpectedValue);
            }

            foreach (var match in request.InMatches)
            {
                _builder.AddIn(match.Field, match.Values);
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
                _builder.AddComposite(composite);
            }

            return _builder.Build();
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
