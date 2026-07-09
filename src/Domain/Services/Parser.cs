using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Interface;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace NX_lims_Softlines_Command_System.src.Domain.Services
{
    /// <summary>
    /// 领域服务：规则解析器
    /// 协调 Token 序列与 Formula 范式的匹配
    /// 负责业务规则校验和语义推导
    /// </summary>
    public class Parser: IParser,IScopedDependency
    {

        private readonly IConditionPatternSerializer _serializer;

        public Parser(IConditionPatternSerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        /// <summary>
        /// 解析规则文本
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="formula"></param>
        /// <returns></returns>
        public ParsedRule Parse(IReadOnlyList<Token> tokens,Formula formula)
        {
            // 1. 校验推导符
            var arrowIndex = FindRangeOperator(tokens);

            // 2. 分割左右
            var leftTokens = tokens.Take(arrowIndex).ToList();
            var rightTokens = tokens.Skip(arrowIndex + 1).ToList();

            // 3. 按 Formula 范式分割槽位
            var slotValues = SplitByDelimiter(leftTokens, formula.ExpressionTemplate);

            // 4. 校验槽位数量
            ValidateSlotCount(slotValues, formula.ConditionFields);

            // 5. 构建 EqualMatches，由于Formula为完全配置其他类型的ConditionPattern所以需要等待后续扩展
            var equals = BuildEqualConditions(slotValues, formula.ConditionFields);

            // 6. 用 Serializer 组装 Pattern JSON
            var patternJson = _serializer.BuildPattern(equals: equals);

            // 7. 构建结果
            var resultValue = string.Join("", rightTokens.Select(t => t.Value));

            return new ParsedRule
            {
                ConditionPatternJson = patternJson,                               //提交给conditionPatternBuilder用于构建ConditionPattern对象
                ResultValue = resultValue,                                               //用于构建聚合根ParamValue字段
                SourceText = string.Join("", tokens.Select(t => t.Value)) //原文本
            };
        }

        /// <summary>
        /// 寻找推导符 → 的位置
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private int FindRangeOperator(IReadOnlyList<Token> tokens)
        {
            var index = tokens.ToList().FindIndex(t => t.Type == TokenType.RangeOperator);
            if (index < 0) throw new Exception("规则缺少推导符 →");
            return index;
        }

        /// <summary>
        /// 槽位分隔
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="delimiter"></param>
        /// <returns></returns>
        private List<List<Token>> SplitByDelimiter(List<Token> tokens, string delimiter)
        {
            // 按 Delimiter 分割槽位
            var slots = new List<List<Token>>();
            var current = new List<Token>();

            foreach (var token in tokens)
            {
                // StringLiteral 直接加入当前槽位，不参与分隔
                if (token.Type == TokenType.StringLiteral)
                {
                    current.Add(token);
                    continue;
                }
                //delimiter为分隔符，当前isMatch将所有ArithmeticOperator当作分隔符
                //当"-"表示字符串连接符号时可能会因为ArithmeticOperator而被误判为分隔符，导致槽位拆分错误
                //在这里引入引号包裹，包裹内的"-"不再被当作ArithmeticOperator处理,而是被当作StringLiteral处理

                // 关键调试：看判断条件是否满足
                var isMatch = token.Type == TokenType.ArithmeticOperator;

                Console.WriteLine($"Token: [{token.Type}] '{token.Value}' | isMatch={isMatch}");

                if (isMatch)
                {
                    slots.Add(current);
                    current = new List<Token>();
                }
                else
                {
                    current.Add(token);
                }
            }

            if (current.Any()) slots.Add(current);

            // 调试输出结果
            Console.WriteLine($"Total slots: {slots.Count}");
            for (int i = 0; i < slots.Count; i++)
                Console.WriteLine($"Slot {i}: {string.Join(", ", slots[i].Select(t => t.Value))}");

            //输出推导符左侧的槽位内容
            return slots;
        }


        /// <summary>
        /// 校验槽位数量与 Formula.ConditionFields 匹配
        /// </summary>
        private void ValidateSlotCount(
            List<List<Token>> slotValues,
            List<string> conditionFields)
        {
            if (slotValues.Count != conditionFields.Count)
                throw new Exception(
                    $"范式要求 {conditionFields.Count} 个条件（{string.Join(", ", conditionFields)}），" +
                    $"实际有 {slotValues.Count} 个");
        }


        /// <summary>
        /// 构建等值条件列表
        /// </summary>
        private List<(string field, object? value)> BuildEqualConditions(
            List<List<Token>> slotValues,
            List<string> conditionFields)
        {
            var equals = new List<(string field, object? value)>();

            for (int i = 0; i < conditionFields.Count; i++)
            {
                var fieldName = conditionFields[i];

                var rawValue = string.Join("", slotValues[i].Select(t => t.Value)).Trim();

                // 类型转换（根据字段名推断）
                //var value = ConvertValue(fieldName, rawValue);

                equals.Add((fieldName, rawValue));
            }

            return equals;
        }
    }
}
