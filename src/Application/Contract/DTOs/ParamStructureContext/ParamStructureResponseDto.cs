namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamStructureContext
{
    public record ParamStructureResponseDto
    {
        //根据前端要求适配
        /// <summary>
        /// 参数结构id
        /// </summary>
        public string ParamStructureId { get; set; } = string.Empty;

        /// <summary>
        /// 标准族id
        /// </summary>
        public IEnumerable<string>? StandardFamilyIds { get; set; }

        /// <summary>
        /// 所属公式Id
        /// </summary>
        public string FormulaId { get; set; } = string.Empty;

        /// <summary>
        /// 规则id
        /// </summary>
        public IEnumerable<string>? RuleIds { get; set; }

        /// <summary>
        /// 买家d
        /// </summary>
        public IEnumerable<string>? BuyerCodes { get; set; }

        /// <summary>
        /// 参数名称
        /// </summary>
        public string ParamName { get; set; } = string.Empty;

        /// <summary>
        /// 生效时间
        /// </summary>
        public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 所属层级
        /// </summary>
        public string EngineLayer { get; set; } = string.Empty;

        /// <summary>
        /// 结构
        /// </summary>
        public SchemaDto ParamSchema { get; set; } = new SchemaDto();
    }
}
