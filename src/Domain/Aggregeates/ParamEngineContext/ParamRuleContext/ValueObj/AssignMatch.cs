namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj
{
    /// <summary>
    /// 赋值匹配：从条件池的指定字段取值，作为规则的 Result
    /// 
    /// 语义：
    /// - 该匹配模式不进行"比较"，而是"取值"
    /// - 取值成功后，Match 返回 true，同时 Result 被动态赋值为取到的值
    /// - 多个 AssignMatch 之间是 AND 关系（都需要成功取值）
    /// 
    /// 典型场景：
    /// - 买家指定值透传：Ballast = BuyerBallastValue
    /// - 条件字段直传：WashingTemperature = Temperature
    /// - 带转换的透传：Temperature(40) → "40°C"
    /// </summary>
    public class AssignMatch
    {
        /// <summary>
        /// 源字段路径（从条件池中取值）
        /// 支持路径如 "FiberInfo.Composition"
        /// </summary>
        public string SourceFieldPath { get; set; } = string.Empty;

        /// <summary>
        /// 是否必填
        /// - true：取不到值 → 匹配失败（返回 false）
        /// - false：取不到值 → 使用 DefaultValue
        /// </summary>
        public bool IsRequired { get; set; } = true;

        /// <summary>
        /// 默认值（当 IsRequired = false 且取不到值时使用）
        /// </summary>
        public object? DefaultValue { get; set; }

        public AssignMatch() { }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="sourceFieldPath"></param>
        /// <param name="transformer"></param>
        /// <param name="isRequired"></param>
        /// <param name="defaultValue"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public AssignMatch(
            string sourceFieldPath,
            bool isRequired = true, 
            object? defaultValue = null)
        {
            SourceFieldPath = sourceFieldPath ?? throw new ArgumentNullException(nameof(sourceFieldPath));
            IsRequired = isRequired;
            DefaultValue = defaultValue;
        }
    }
}
