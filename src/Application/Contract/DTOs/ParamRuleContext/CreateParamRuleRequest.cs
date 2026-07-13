namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamRuleContext
{
    public record CreateParamRuleRequest
    {
        /// <summary>
        /// 规则 ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 所属公式
        /// </summary>
        public string FormulaId { get; set; } = string.Empty;

        /// <summary>
        /// 所属参数结构
        /// </summary>
        public string ParamStructureId { get; set; } = string.Empty;

        /// <summary>
        /// 所属标准族
        /// </summary>
        public string StandardFamilyId { get; set; } = string.Empty;

        /// <summary>
        /// 参数名
        /// </summary>
        public string ParamName { get; set; } = string.Empty;

        /// <summary>
        /// 结果
        /// </summary>
        public string ParamResult { get; set; } = string.Empty;

        /// <summary>
        /// 命中停止
        /// </summary>
        public bool StopOnMatch { get; set; }

        /// <summary>
        /// 优先级
        /// </summary>
        public int Priority { get; set; }
        public List<EqualMatchDto> EqualMatches { get; set; } = new();
        public List<ComparisonMatchDto> ComparisonMatches { get; set; } = new();
        public List<InMatchDto> InMatches { get; set; } = new();
        public List<CompositeConditionDto> CompositeMatches { get; set; } = new();
    }

    /// <summary>
    /// 等值匹配
    /// </summary>
    public class EqualMatchDto
    {
        public string Field { get; set; } = string.Empty;
        public object Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// 比较匹配
    /// </summary>
    public class ComparisonMatchDto
    {
        public string FieldPath { get; set; } = string.Empty;
        public string Operator { get; set; } = string.Empty;
        public object ExpectedValue { get; set; } = string.Empty;
    }

    /// <summary>
    /// 包含匹配
    /// </summary>
    public class InMatchDto
    {
        public string Field { get; set; } = string.Empty;
        public List<object> Values { get; set; } = new();
    }

    /// <summary>
    /// 复合匹配
    /// </summary>
    public class CompositeConditionDto
    {
        public string Logic { get; set; } = string.Empty;
        public List<string> FieldNames { get; set; } = new();
        public List<ComparisonMatchDto> SubConditions { get; set; } = new();
    }
}
