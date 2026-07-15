namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamRuleContext
{
    public record NaturalLanguageRuleRequest
    {
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
        /// 标准族id
        /// </summary>
        public string StandardFamilyId { get; set; } = string.Empty;

        /// <summary>
        /// 参数字段名称
        /// </summary>
        public string ParamName { get; set; } = string.Empty;

        /// <summary>
        /// 优先级数
        /// </summary>
        public int Priority { get; set; } = 1;
    }
}
