namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Util
{
    public class Token
    {
        /// <summary>
        /// Token的值
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Token的类型
        /// </summary>
        public TokenType Type { get; }

        /// <summary>
        /// Token的位置信息（用于错误报告）
        /// </summary>
        public int Position { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="value">Token的值</param>
        /// <param name="type">Token的类型</param>
        /// <param name="position">Token的位置</param>
        public Token(string value, TokenType type, int position = 0)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            Type = type;
            Position = position;
        }

        public override string ToString() => $"[{Type}] {Value} (at {Position})";
    }

    /// <summary>
    /// Token类型枚举
    /// </summary>
    public enum TokenType
    {
        /// <summary>
        /// 标识符：字母开头的单词或短语（如 Woven, Flat, dry, ISO6330, Null）
        /// </summary>
        Identifier,

        /// <summary>
        /// 数值：整数或小数（如 30, 40.5, 60）
        /// </summary>
        Number,

        /// <summary>
        /// 单位符号：℃, %, min, g, m, s 等
        /// </summary>
        Unit,

        /// <summary>
        /// 逻辑运算符：AND, OR, NOT
        /// </summary>
        LogicalOperator,

        /// <summary>
        /// 比较运算符：>, <, >=, <=, =, !=
        /// </summary>
        ComparisonOperator,

        /// <summary>
        /// 算术运算符：+, -, *, /
        /// </summary>
        ArithmeticOperator,

        /// <summary>
        /// 范围/推导运算符：→, ->, =>, ~, to
        /// </summary>
        RangeOperator,

        /// <summary>
        /// 括号：(, )
        /// </summary>
        Parenthesis,

        /// <summary>
        /// 分隔符：, ; :
        /// </summary>
        Separator,

        /// <summary>
        /// 赋值/推导标记：=, :=, =>
        /// </summary>
        Assignment,

        /// <summary>
        /// 字符串字面量：用引号包裹的文本
        /// </summary>
        StringLiteral,

        /// <summary>
        /// 未知/未识别类型
        /// </summary>
        Unknown
    }
}
