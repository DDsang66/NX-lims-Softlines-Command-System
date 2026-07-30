using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Interface;
using System.Text.RegularExpressions;

namespace NX_lims_Softlines_Command_System.src.Domain.Services
{
    /// <summary>
    /// 领域服务：Tokenizer 协调层
    /// </summary>
    public sealed class Tokenizer : ITokenizer, IScopedDependency
    {
        private readonly IRuleTokenizer _ruleTokenizer;

        public Tokenizer(IRuleTokenizer ruleTokenizer)
        {
            _ruleTokenizer = ruleTokenizer ?? throw new ArgumentNullException(nameof(ruleTokenizer));
        }

        /// <summary>
        /// Tokenize
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public IReadOnlyList<Token> Tokenize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("规则文本不能为空", nameof(text));

            // 支持更多推导符检查（包括单词 to 与 ~）
            if (!HasRangeOperator(text))
                throw new ArgumentException("规则文本必须包含推导符 →", nameof(text));

            if (!IsParenthesesBalanced(text))
                throw new ArgumentException("规则文本括号不匹配", nameof(text));

            var tokens = _ruleTokenizer.Split(text);

            ValidateTokenSequence(tokens);

            return tokens;
        }

        /// <summary>
        /// 扩展：识别独立单词 "to" 与波浪号 "~"
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static bool HasRangeOperator(string text)
            => text.Contains('→')
               || text.Contains("->")
               || text.Contains("=>")
               || text.Contains("~")
               || Regex.IsMatch(text, @"\bto\b", RegexOptions.IgnoreCase);

        /// <summary>
        /// 验证括号是否匹配
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static bool IsParenthesesBalanced(string text)
        {
            int depth = 0;
            foreach (var c in text)
            {
                if (c == '(') depth++;
                if (c == ')') depth--;
                if (depth < 0) return false;
            }
            return depth == 0;
        }

        /// <summary>
        /// 验证规则文本中的推导符数量和位置
        /// </summary>
        /// <param name="tokens"></param>
        /// <exception cref="ArgumentException"></exception>
        private static void ValidateTokenSequence(IReadOnlyList<Token> tokens)
        {
            var arrowCount = tokens.Count(t => t.Type == TokenType.RangeOperator);
            if (arrowCount != 1)
                throw new ArgumentException($"规则必须包含且仅包含一个推导符，当前有 {arrowCount} 个");

            var arrowIndex = tokens.ToList().FindIndex(t => t.Type == TokenType.RangeOperator);
            if (arrowIndex == 0 || arrowIndex == tokens.Count - 1)
                throw new ArgumentException("推导符 → 不能位于规则开头或结尾");
        }
    }
}

