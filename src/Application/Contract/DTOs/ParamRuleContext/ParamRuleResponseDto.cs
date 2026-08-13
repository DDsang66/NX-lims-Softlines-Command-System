namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamRuleContext
{
    /// <summary>
    /// 参数规则响应DTO
    /// </summary>
    public record ParamRuleResponseDto
    {
        /// <summary>
        /// 规则ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 所属公式ID
        /// </summary>
        public string? FormulaId { get; set; }

        /// <summary>
        /// 参数结构id
        /// </summary>
        public string? ParamStructureId { get; set; }

        /// <summary>
        /// 标准族id
        /// </summary>
        public string? StandardFamilyId { get; set; }

        /// <summary>
        /// 参数名
        /// </summary>
        public string ParamName { get; set; }

        /// <summary>
        /// 优先级
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 等值匹配条件
        /// </summary>
        public List<EqualMatchDto> EqualMatches { get; set; } = new();

        /// <summary>
        /// 比较匹配条件
        /// </summary>
        public List<ComparisonMatchDto> ComparisonMatches { get; set; } = new();

        /// <summary>
        /// 集合匹配条件
        /// </summary>
        public List<InMatchDto> InMatches { get; set; } = new();

        /// <summary>
        /// 复合条件
        /// </summary>
        public List<CompositeConditionDto> CompositeMatches { get; set; } = new();

        /// <summary>
        /// 结果值
        /// </summary>
        public object? ResultValue { get; set; }

        /// <summary>
        /// 结果备注
        /// </summary>
        public string? ResultNotes { get; set; }

        /// <summary>
        /// 命中后是否停止匹配
        /// </summary>
        public bool StopOnMatch { get; set; }
    }
}

