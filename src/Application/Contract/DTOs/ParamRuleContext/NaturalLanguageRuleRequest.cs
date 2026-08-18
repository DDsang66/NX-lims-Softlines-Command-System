namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamRuleContext
{
    public record NaturalLanguageRuleRequest
    {
        /// <summary>
        /// 规则 ID
        /// </summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>
        /// 规则文本
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 关联规则id
        /// </summary>
        public string FormulaId { get; set; } = string.Empty;

        /// <summary>
        /// 参数结构id
        /// </summary>
        public string ParamStructureId { get; set; } = string.Empty;

        /// <summary>
        /// 参数字段名称
        /// </summary>
        public string ParamName { get; set; } = string.Empty;

        /// <summary>
        /// 优先级数
        /// </summary>
        public int Priority { get; set; } = 1;

        /// <summary>
        /// 命中停止
        /// </summary>
        public bool StopOnMatch { get; set; }

        /// <summary>
        /// 所属层级
        /// </summary>
        public string EngineLayer { get; set; } = string.Empty;
    }
}
