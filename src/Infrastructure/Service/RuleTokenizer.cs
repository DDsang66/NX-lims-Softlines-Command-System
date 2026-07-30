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
        /// 1. 字符串字面量
        /// 2. 数值
        /// 3. 单位
        /// 4. 多字符运算符
        /// 5. 单字符运算符（包含波浪号 ~ 和点 .）
        /// 6. 标识符（允许点号用于路径如 A.B）
        /// </summary>
        private static readonly Regex TokenPattern = new Regex(
            @"(?:""[^""]*"")" +               // 1. 字符串字面量
            @"|(\d+\.?\d*)" +                // 2. 数值
            @"|(℃|°F|%|min|g|m|s)" +        // 3. 单位（扩展 °F）
            @"|(→|->|=>|>=|<=|==|!=)" +     // 4. 多字符运算符
            @"|([+\-*/<>=,;():~\{\}])" +     // 5. 单字符运算符（添加 ~ 和 { }）
            @"|([\w\.]+)",                  // 6. 标识符（允许点号）
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
        private static TokenType DetermineTokenType(string value)
        {
            if (value.StartsWith("\"") && value.EndsWith("\""))
                return TokenType.StringLiteral;

            if (decimal.TryParse(value, out _))
                return TokenType.Number;

            // 单位判断（可后续扩展到配置）
            if (value is "℃" or "°F" or "%" or "min" or "g" or "m" or "s")
                return TokenType.Unit;

            return value switch
            {
                "AND" or "OR" or "NOT" => TokenType.LogicalOperator,
                "→" or "->" or "=>" or "to" or "~" => TokenType.RangeOperator,
                ">=" or "<=" or "==" or "!=" or ">" or "<" or "=" => TokenType.ComparisonOperator,
                "+" or "-" or "*" or "/" => TokenType.ArithmeticOperator,
                "(" or ")" or "{" or "}" => TokenType.Parenthesis, // <-- 支持大括号
                "," or ";" or ":" => TokenType.Separator,
                ":=" => TokenType.Assignment,
                _ => TokenType.Identifier
            };
        }
    }
}

