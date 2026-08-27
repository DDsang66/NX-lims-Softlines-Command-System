using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;

namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamFormulaContext
{
    public record FormulaResponseDto
    {
        /// <summary>
        /// 公式ID
        /// </summary>
        public string Id { get; set; } = string.Empty;  // "BallastDerivation"

        /// <summary>
        /// 参数结构 Id
        /// </summary>
        public List<string?> ParamStrurctureIds { get; set; } = new();

        /// <summary>
        /// 标准族 Id
        /// </summary>
        public List<string?> StandardFamilyIds { get; set; } = new();

        /// <summary>
        /// 买家Id
        /// </summary>
        public List<string?> BuyerCodes { get; set; } = new();

        /// <summary>
        /// 公式名称
        /// </summary>
        public string Name { get; set; } = string.Empty;  // "BallastDerivation"

        /// <summary>
        /// 生成参数名
        /// </summary>
        public string ParamName { get; set; } = string.Empty;  // 生成的参数名 "Ballast"

        /// <summary>
        /// 条件字段
        /// </summary>
        public List<string> ConditionFields { get; set; } = new(); // ["FiberDominantType", "BuyerSpecified"]等具体语义的字段名(不可再切割)

        /// <summary>
        /// 公式模板
        /// </summary>
        public string ExpressionTemplate { get; set; } = string.Empty; // "FiberDominantType + BuyerSpecified ->Ballst" 范式样本

        /// <summary>
        /// 描述
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 版本号
        /// </summary>
        public int Version { get; set; }  // 版本号

        /// <summary>
        /// 生效日期
        /// </summary>
        public DateTime EffectiveDate { get; set; }  // 生效日期

        /// <summary>
        /// 公式是否启用
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// 所属引擎层
        /// </summary>
        public string EngineLayer { get; set; } = string.Empty;  // "ParamEngine"
    }
}
