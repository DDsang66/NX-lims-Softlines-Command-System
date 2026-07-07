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
        /// 条件类型（如：Type）
        /// </summary>
        ConditionType,

        /// <summary>
        /// 条件值（如：A、B、C）
        /// </summary>
        ConditionValue,

        /// <summary>
        /// 温度值（如：+40℃）
        /// </summary>
        Temperature,

        /// <summary>
        /// 范围分隔符（如：→、-）
        /// </summary>
        RangeOperator,

        /// <summary>
        /// 运算符（如：+、-、*、/）
        /// </summary>
        ArithmeticOperator,

        /// <summary>
        /// 数字
        /// </summary>
        Number,

        /// <summary>
        /// 单位（如：℃、%）
        /// </summary>
        Unit,

        /// <summary>
        /// 逻辑运算符（如：AND、OR、NOT）
        /// </summary>
        LogicalOperator,

        /// <summary>
        /// 比较运算符（如：>、<、=）
        /// </summary>
        ComparisonOperator,

        /// <summary>
        /// 括号
        /// </summary>
        Parenthesis,

        /// <summary>
        /// 未知类型
        /// </summary>
        Unknown
    }
}
