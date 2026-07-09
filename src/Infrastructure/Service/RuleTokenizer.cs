using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Interface;
using System.Text.RegularExpressions;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Service
{
    /// <summary>
    /// 基础设施层：底层词法拆分实现
    /// 纯技术实现，无业务逻辑，无异常处理
    /// </summary>
    public sealed class RuleTokenizer : IRuleTokenizer, IScopedDependency
    {
        /// <summary>
        /// 原子化正则：每个匹配项是不可再分的基础单元
        /// </summary>
        private static readonly Regex TokenPattern = new Regex(
            @"(?:""[^""]*"")" +           // 1. 字符串字面量
            @"|(\d+\.?\d*)" +             // 2. 数值
            @"|(℃|%|min|g|m|s)" +        // 3. 单位
            @"|(→|->|=>|>=|<=|==|!=)" +    // 4. 多字符运算符
            @"|([+\-*/<>=,;():])" +       // 5. 单字符运算符
            @"|(\w+)",                    // 6. 标识符
            RegexOptions.Compiled);

        public IReadOnlyList<Token> Split(string text)
        {
            var tokens = new List<Token>();

            foreach (Match match in TokenPattern.Matches(text))
            {
                if (!match.Success) continue;

                var value = match.Value.Trim();
                if (string.IsNullOrEmpty(value)) continue;

                var token = new Token(
                    value: value,
                    type: DetermineTokenType(value),
                    position: match.Index
                );

                tokens.Add(token);
            }

            return tokens;
        }

        /// <summary>
        /// 根据值确定类型
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static TokenType DetermineTokenType(string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\""))
                return TokenType.StringLiteral;

            if (decimal.TryParse(value, out _))
                return TokenType.Number;

            //后续从单位符号库查询，禁止使用硬编码
            if (value is "℃" or "°F" or "%" or "min" or "g" or "m" or "s")    
                return TokenType.Unit;

            return value switch
            {
                "AND" or "OR" or "NOT" => TokenType.LogicalOperator,
                "→" or "->" or "=>" or "to" or "~" or "→" => TokenType.RangeOperator,
                ">=" or "<=" or "==" or "!=" or ">" or "<" or "=" => TokenType.ComparisonOperator,
                "+" or "-" or "*" or "/" => TokenType.ArithmeticOperator,
                "(" or ")" => TokenType.Parenthesis,
                "," or ";" or ":" => TokenType.Separator,
                ":=" => TokenType.Assignment,
                _ => TokenType.Identifier
            };
        }
    }
}

