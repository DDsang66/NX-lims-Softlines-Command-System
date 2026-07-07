using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Services;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Interface;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Service
{

    //后续需要重构，只负责语法分析，不构建领域对象
    public class RuleParser : IRuleParser, IScopedDependency
    {
        public ConditionPattern Parse(List<Token> tokens)
        {
            // 验证token列表
            if (tokens == null || tokens.Count < 5)
            {
                throw new ArgumentException("Invalid token list for rule parsing", nameof(tokens));
            }

            var pattern = new ConditionPattern();
            var builder = new ConditionPatternBuilder();

            // 解析示例: "Type B +40℃ → 41℃"
            // 期望的Token流: [ConditionType, ConditionValue, Temperature, RangeOperator, Temperature]

            // 解析条件部分
            var conditionTypeToken = tokens[0];
            var conditionValueToken = tokens[1];

            // 验证token类型
            if (conditionTypeToken.Type != TokenType.ConditionType ||
                conditionValueToken.Type != TokenType.ConditionValue)
            {
                throw new ArgumentException("Invalid token types for condition");
            }

            // 解析温度范围
            var minTempToken = tokens[2];
            var maxTempToken = tokens[4];
            var rangeOperatorToken = tokens[3];

            // 验证温度token类型
            if (minTempToken.Type != TokenType.Temperature ||
                maxTempToken.Type != TokenType.Temperature ||
                rangeOperatorToken.Type != TokenType.RangeOperator)
            {
                throw new ArgumentException("Invalid token types for temperature range");
            }

            // 构建条件模式
            builder.AddEqual(conditionTypeToken.Value, conditionValueToken.Value);
            builder.AddComparison("Temperature", ComparisonOperator.GreaterThanOrEqual, ParseTemperature(minTempToken.Value));
            builder.AddComparison("Temperature", ComparisonOperator.LessThan, ParseTemperature(maxTempToken.Value));

            return builder.Build();
        }

        private double ParseTemperature(string temp)
        {
            if (string.IsNullOrWhiteSpace(temp))
                throw new ArgumentException("Temperature value cannot be empty", nameof(temp));

            var value = temp.Replace("℃", "");
            if (!double.TryParse(value, out double result))
            {
                throw new ArgumentException($"Invalid temperature value: {temp}", nameof(temp));
            }

            return result;
        }
    }
}
