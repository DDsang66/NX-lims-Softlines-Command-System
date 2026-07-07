using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using System.Text.RegularExpressions;

namespace NX_lims_Softlines_Command_System.src.Domain.Services
{
    public class Tokenizer: ITokenizer,IScopedDependency
    {
        private readonly Dictionary<string, TokenType> _keywordMap;
        private readonly HashSet<char> _unitChars;

        public Tokenizer()
        {
            // 初始化关键字映射
            _keywordMap = new Dictionary<string, TokenType>(StringComparer.OrdinalIgnoreCase)
            {
                ["Type"] = TokenType.ConditionType,
                ["AND"] = TokenType.LogicalOperator,
                ["OR"] = TokenType.LogicalOperator,
                ["NOT"] = TokenType.LogicalOperator,
                ["→"] = TokenType.RangeOperator,
                ["-"] = TokenType.RangeOperator,
                ["+"] = TokenType.ArithmeticOperator,
                ["-"] = TokenType.ArithmeticOperator,
                ["*"] = TokenType.ArithmeticOperator,
                ["/"] = TokenType.ArithmeticOperator,
                [">"] = TokenType.ComparisonOperator,
                ["<"] = TokenType.ComparisonOperator,
                ["="] = TokenType.ComparisonOperator
            };

            // 初始化单位字符集合
            _unitChars = new HashSet<char> { '℃', '%', '°', 'C' };
        }

        public List<Token> Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text cannot be empty", nameof(text));

            var tokens = new List<Token>();
            var position = 0;
            var length = text.Length;

            while (position < length)
            {
                // 跳过空白字符
                if (char.IsWhiteSpace(text[position]))
                {
                    position++;
                    continue;
                }

                // 尝试匹配最长的可能token
                var token = MatchLongestToken(text, position);
                if (token != null)
                {
                    tokens.Add(token);
                    position += token.Value.Length;
                }
                else
                {
                    // 无法识别的token
                    throw new TokenizationException(
                        $"Unrecognized token at position {position}: '{text[position]}'");
                }
            }

            return tokens;
        }

        public bool ValidateTokens(List<Token> tokens)
        {
            if (tokens == null || tokens.Count == 0)
                return false;

            // 检查基本语法结构
            // 示例：Type B +40℃ → 41℃
            // 期望的结构：[ConditionType, ConditionValue, Temperature, RangeOperator, Temperature]

            if (tokens.Count < 5)
                return false;

            // 检查第一个token是否为条件类型
            if (tokens[0].Type != TokenType.ConditionType)
                return false;

            // 检查第二个token是否为条件值
            if (tokens[1].Type != TokenType.ConditionValue)
                return false;

            // 检查温度token
            if (tokens[2].Type != TokenType.Temperature || tokens[4].Type != TokenType.Temperature)
                return false;

            // 检查范围运算符
            if (tokens[3].Type != TokenType.RangeOperator)
                return false;

            return true;
        }

        private Token MatchLongestToken(string text, int position)
        {
            // 尝试匹配关键字
            foreach (var keyword in _keywordMap.Keys.OrderByDescending(k => k.Length))
            {
                if (text.Substring(position, Math.Min(keyword.Length, text.Length - position))
                    .Equals(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return new Token(keyword, _keywordMap[keyword], position);
                }
            }

            // 尝试匹配数字
            var numberMatch = Regex.Match(text.Substring(position), @"^[-+]?\d+(\.\d+)?");
            if (numberMatch.Success)
            {
                var value = numberMatch.Value;
                // 检查是否包含单位
                if (position + value.Length < text.Length && _unitChars.Contains(text[position + value.Length]))
                {
                    value += text[position + value.Length].ToString();
                    return new Token(value, TokenType.Temperature, position);
                }
                return new Token(value, TokenType.Number, position);
            }

            // 尝试匹配条件值（单个字母）
            if (char.IsLetter(text[position]))
            {
                return new Token(text[position].ToString(), TokenType.ConditionValue, position);
            }

            // 尝试匹配单位
            if (_unitChars.Contains(text[position]))
            {
                return new Token(text[position].ToString(), TokenType.Unit, position);
            }

            return null;
        }
    }

    /// <summary>
    /// 词法分析异常
    /// </summary>
    public class TokenizationException : Exception
    {
        public TokenizationException(string message) : base(message) { }
        public TokenizationException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}

