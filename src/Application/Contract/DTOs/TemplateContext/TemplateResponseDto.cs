namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TemplateContext
{
    public record TemplateResponseDto
    {
        public string TemplateId { get; init; } = string.Empty;
        public string TemplateName { get; init; } = string.Empty;
        public string TemplateUrl { get; init; } = string.Empty;
        public string Site { get; init; } = string.Empty;
        public string Status { get; init; } = string.Empty;
        public string FileType { get; init; } = string.Empty;
        public string BusinessCategory { get; init; } = string.Empty;
        public int Version { get; init; }
        public DateTime UpdateAt { get; init; }

        // ==================== 新增 ====================

        /// <summary>
        /// 模板索引（key-value）
        /// </summary>
        public Dictionary<string, object> TemplateIndex { get; init; } = new();

        /// <summary>
        /// 测试条件文本模板集合
        /// </summary>
        public List<TestConditionTextTemplateResponseDto> TestConditionTextTemplates { get; init; } = new();

        /// <summary>
        /// 模板结构
        /// </summary>
        public TemplateStructureResponseDto? TemplateStructure { get; init; }
    }

    /// <summary>
    /// 测试条件文本模板的响应 DTO
    /// </summary>
    public record TestConditionTextTemplateResponseDto
    {
        /// <summary>
        /// 模板索引（key-value）
        /// </summary>
        public Dictionary<string, object> TemplateIndex { get; init; } = new();

        /// <summary>
        /// 文本模板
        /// </summary>
        public string Text { get; init; } = string.Empty;
    }

    /// <summary>
    /// 模板结构的响应 DTO
    /// </summary>
    public record TemplateStructureResponseDto
    {
        public int TestConditionCount { get; init; }
        public int TestMethodCount { get; init; }
        public int SampleDataAreaCount { get; init; }
        public int SampleResultAreaCount { get; init; }
        public int AfterWashDataCount { get; init; }
    }
}

