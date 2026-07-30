namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TestItemContext
{
    public record TestItemResponseDto
    {
        /// <summary>
        /// TestItemId
        /// </summary>
        public string Id { get; set; }

        ///<summary>
        /// 英文名称
        ///</summary>
        public string NameEn { get; set; } = string.Empty;

        /// <summary>
        /// 中文名称
        /// </summary>
        public string NameChn { get; set; } = string.Empty;

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 测试组别
        /// </summary>
       public string Group { get; set; } = string.Empty;

        /// <summary>
        /// 是否在能力范围内
        /// </summary>
        public bool IsFeasible { get; set; }

        /// <summary>
        /// 测试项目级别的参数要求定义
        /// </summary>
        public IEnumerable<ParamRequireDefinitionDto> ParamRequireDefinitions { get; set; } = new List<ParamRequireDefinitionDto>();
        /// <summary>
        /// 状态
        /// </summary>
        public string Status { get; set; }
    }
}
