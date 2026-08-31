using DocumentFormat.OpenXml.CustomXmlSchemaReferences;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamStructureContext
{
    public record AddParamStructureDto
    {
        /// <summary>
        /// 参数结构id
        /// </summary>
        public string ParamStructureId { get; set; } =string.Empty;

        /// <summary>
        /// 标准族id
        /// </summary>
        public IEnumerable<string>? StandardFamilyIds { get; set; }

        /// <summary>
        /// 所属公式Id
        /// </summary>
        public string FormulaId{ get; set; } = string.Empty;

        /// <summary>
        /// 规则id
        /// </summary>
        public IEnumerable<string>? RuleIds { get; set; }

        /// <summary>
        /// 买家id
        /// </summary>
        public IEnumerable<string>? BuyerIds { get; set; }

        /// <summary>
        /// 参数名称
        /// </summary>
        public string ParamName { get; set; } = string.Empty;

        /// <summary>
        /// 所属层级
        /// </summary>
        public string EngineLayer { get; set; } = string.Empty;

        /// <summary>
        /// 生效时间
        /// </summary>
        public DateTime EffectiveDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 是否可以作为条件使用
        /// </summary>
        public bool IsEligibleAsCondition { get; set; }

        /// <summary>
        /// 结构
        /// </summary>
        public SchemaDto ParamSchema { get; set; } = new SchemaDto();
    }


    /// <summary>
    /// 结构
    /// </summary>
    public record SchemaDto 
    {
        public ParamDefinitionDto RequiredParam { get; set; } = new ParamDefinitionDto();
        public List<ConditionRequirementDto> ConditionRequirements { get; set; } = new List<ConditionRequirementDto>();
        public Dictionary<string, ParamLimitationDto> Limitations { get; set; } = new Dictionary<string, ParamLimitationDto>();
    }

    /// <summary>
    /// 参数定义
    /// </summary>
    public record ParamDefinitionDto 
    {
        /// <summary>
        /// 对应参数名称
        /// </summary>
        public string Name { get; set; }  = string.Empty;

        /// <summary>
        /// 类型
        /// </summary>
        public string ValueType { get; set; } = typeof(string).FullName ?? string.Empty; //  // typeof(string)

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 是否可为空
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// 默认值
        /// </summary>
        public object DefaultValue { get; set; }   // 补偿机制用
    }

    /// <summary>
    /// 单元条件结构
    /// </summary>
    public record ConditionRequirementDto 
    {
        /// <summary>
        /// 条件名称
        /// </summary>
        public string FieldName { get; set; } = string.Empty;  // "FiberDominantType"
        
        /// <summary>
        /// 条件类型
        /// </summary>
        public string FieldType { get; set; } = typeof(string).FullName ?? string.Empty;   // typeof(string)

        /// <summary>
        /// 是否必须
        /// </summary>
        public bool IsRequired { get; set; }

        /// <summary>
        /// 这个条件的可选值
        /// </summary>
        public List<object> AllowedValues { get; set; } = new List<object>();
    }

    /// <summary>
    /// 单元参数限值
    /// </summary>
    public record ParamLimitationDto 
    {
        /// <summary>
        /// 类型
        /// </summary>
        public string ValueType { get; set; } = typeof(string).FullName ?? string.Empty; //  // typeof(string)

        /// <summary>
        /// 参数可选值
        /// </summary>
        public List<object>? AllowedValues { get; set; } = null;

        /// <summary>
        /// 参数最小值
        /// </summary>
        public object? Min { get; set; } = null;

        /// <summary>
        /// 最大值
        /// <summary>
        public object? Max { get; set; } = null;
    }
}
