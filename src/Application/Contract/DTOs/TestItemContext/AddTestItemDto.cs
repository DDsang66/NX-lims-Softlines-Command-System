namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TestItemContext
{
    public record AddTestItemDto
    {
        /// <summary>
        /// id
        /// </summary>
        public string TestItemId { get; set; } = string.Empty;

        /// <summary>
        /// 英文名称
        /// </summary>
        public string TestItemNameEn { get; set; } = string.Empty;

        /// <summary>
        /// 中文名称
        /// </summary>
        public string TestItemNameChn { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 测试组别
        /// </summary>
        public int Group { get; set; }

        /// <summary>
        /// 是否在能力范围内
        /// </summary>
        public bool IsFeasible { get; set; }

        /// <summary>
        /// 项目状态
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 项目参数定义
        /// </summary>
        public List<ParamRequireDefinitionDto> ParamRequireDefinitions { get; set; } = new List<ParamRequireDefinitionDto>();
    }
}
