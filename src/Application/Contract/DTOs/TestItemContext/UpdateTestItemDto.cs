using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;

namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TestItemContext
{
    public record UpdateTestItemDto
    {
        /// <summary>
        /// id
        /// </summary>
        public string TestItemId {  get; set; } = string.Empty;

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

    public record ParamRequireDefinitionDto 
    {
        public string ParamName { get; set; } = string.Empty;
        public string ParamTypeName { get;  set; } = "System.String";
        public bool IsRequired { get; set; }

        /// <summary>
        /// 通用默认值（所有标准适用）
        /// </summary>
        public string? UniversalDefault { get; set; }

        /// <summary>
        /// 标准特定默认值（覆盖通用值）
        /// Key: StandardType 字符串
        /// Value: 默认值字符串
        /// </summary>
        public IDictionary<string, string> StandardDefaults { get; set; }
            = new Dictionary<string, string>();
    }
}
