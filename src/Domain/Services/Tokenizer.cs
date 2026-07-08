using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Interface;
using System.Text.RegularExpressions;

namespace NX_lims_Softlines_Command_System.src.Domain.Services
{
    /// <summary>
    /// 领域服务：Tokenizer 协调层
    /// 负责前置检查、异常处理、固定语法结构校验
    /// 底层拆分委托给 Infrastructure 的 IRuleTokenizer
    /// </summary>
    public sealed class Tokenizer : ITokenizer,IScopedDependency
    {
        private readonly IRuleTokenizer _ruleTokenizer;

        public Tokenizer(IRuleTokenizer ruleTokenizer)
        {
            _ruleTokenizer = ruleTokenizer ?? throw new ArgumentNullException(nameof(ruleTokenizer));
        }

        /// <summary>
        /// 词法拆分
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public IReadOnlyList<Token> Tokenize(string text)
        {
            // 1. 前置检查
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("规则文本不能为空", nameof(text));

            // 2. 检查固定语法结构（如必须包含推导符 →）
            if (!HasRangeOperator(text))
                throw new ArgumentException("规则文本必须包含推导符 →", nameof(text));

            // 3. 检查括号匹配
            if (!IsParenthesesBalanced(text))
                throw new ArgumentException("规则文本括号不匹配", nameof(text));

            // 4. 委托底层技术实现进行词法拆分
            var tokens = _ruleTokenizer.Split(text);

            // 5. 后校验：Token 序列合法性
            ValidateTokenSequence(tokens);

            return tokens;
        }

        /// <summary>
        /// 判断规则文本是否包含推导符
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static bool HasRangeOperator(string text)
            => text.Contains('→') || text.Contains("->") || text.Contains("=>");

        /// <summary>
        /// 括号匹配校验
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
        /// Token 序列合法性校验
        /// </summary>
        /// <param name="tokens"></param>
        /// <exception cref="ArgumentException"></exception>
        private static void ValidateTokenSequence(IReadOnlyList<Token> tokens)
        {
            // 检查推导符数量
            var arrowCount = tokens.Count(t => t.Type == TokenType.RangeOperator);
            if (arrowCount != 1)
                throw new ArgumentException($"规则必须包含且仅包含一个推导符，当前有 {arrowCount} 个");

            // 检查推导符位置（不能在最前或最后）
            var arrowIndex = tokens.ToList().FindIndex(t => t.Type == TokenType.RangeOperator);
            if (arrowIndex == 0 || arrowIndex == tokens.Count - 1)
                throw new ArgumentException("推导符 → 不能位于规则开头或结尾");
        }
    }
}

