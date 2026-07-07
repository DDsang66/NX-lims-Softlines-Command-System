using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Interface;
using System.Text.RegularExpressions;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Service
{
    public class RuleTokenizer : IRuleTokenizer, IScopedDependency
    {
        public List<Token> Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Rule text cannot be empty", nameof(text));

            var tokens = new List<Token>();
            var position = 0;
            var regex = new Regex(@"(\w+|[<→,]|\+|\d+℃)");

            foreach (Match match in regex.Matches(text))
            {
                if (match.Success)
                {
                    var token = new Token(
                        match.Value,
                        DetermineTokenType(match.Value),
                        position
                    );
                    tokens.Add(token);
                    position += match.Length;
                }
            }

            return tokens;
        }

        private TokenType DetermineTokenType(string value)
        {
            if (value == "→") return TokenType.RangeOperator;
            if (value == "+") return TokenType.ArithmeticOperator;
            if (value.EndsWith("℃")) return TokenType.Temperature;
            if (int.TryParse(value.Replace("℃", ""), out _)) return TokenType.Number;
            if (Enum.TryParse<ConditionType>(value, true, out _)) return TokenType.ConditionValue;
            return TokenType.Unknown;
        }
    }

    // 保留 ConditionType 枚举，因为它可能特定于基础设施层
    public enum ConditionType
    {
        A,
        B,
        C,
        // 其他条件类型...
    }
}

