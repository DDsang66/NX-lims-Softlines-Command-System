namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs
{
    /// <summary>
    /// 更新参数规则请求DTO
    /// </summary>
    public class UpdateParamRuleRequest
    {
        /// <summary>
        /// 规则ID（不可更新）
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 所属公式ID（可更新）
        /// </summary>
        public string FormulaId { get; set; }

        /// <summary>
        /// 参数名（可更新）
        /// </summary>
        public string ParamName { get; set; }

        /// <summary>
        /// 优先级（可更新）
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 是否激活（可更新）
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 等值匹配条件（可更新）
        /// </summary>
        public List<EqualMatchDto> EqualMatches { get; set; } = new();

        /// <summary>
        /// 比较匹配条件（可更新）
        /// </summary>
        public List<ComparisonMatchDto> ComparisonMatches { get; set; } = new();

        /// <summary>
        /// 集合匹配条件（可更新）
        /// </summary>
        public List<InMatchDto> InMatches { get; set; } = new();

        /// <summary>
        /// 复合条件（可更新）
        /// </summary>
        public List<CompositeConditionDto> CompositeMatches { get; set; } = new();

        /// <summary>
        /// 结果值（可更新）
        /// </summary>
        public object? ResultValue { get; set; }

        /// <summary>
        /// 结果备注（可更新）
        /// </summary>
        public string? ResultNotes { get; set; }
    }
}
