namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamRuleContext
{
    public record UpdateParamRuleTextRequest
    {
        /// <summary>
        /// 规则ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// 所属公式ID
        /// </summary>
        public string FormulaId { get; set; } = string.Empty;

        /// <summary>
        /// 参数名
        /// </summary>
        public string ParamName { get; set; } = string.Empty;

        /// <summary>
        /// 优先级（可更新）
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 是否激活（可更新）
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 规则文本（可更新）
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// 命中后是否停止（可更新）
        /// </summary>
        public bool StopOnMatch { get; set; }

        /// <summary>
        /// 结果备注（可更新）
        /// </summary>
        public string? ResultNotes { get; set; }
    }
}
