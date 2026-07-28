using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;

namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardContext
{
    public record StandaradFamilyResponseDto
    {
        /// <summary>
        /// 标准族id
        /// </summary>
        public string Id { get;  set; }

        /// <summary>
        /// 标准族名称
        /// </summary>
        public string StandardFamilyCode { get; set; } = string.Empty;

        /// <summary>
        /// 标准id集合
        /// </summary>
        public IEnumerable<string?> StandardIds { get; set; }

        /// <summary>
        /// 公式id集合
        /// </summary>
        public IEnumerable<string?> FormulaIds { get; set; }

        /// <summary>
        /// 参数结构id集合
        /// </summary>
        public IEnumerable<string?> ParamStructureIds {  get; set; }

        /// <summary>
        /// 版本
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// 生效日期
        /// </summary>
        public DateTime EffectiveDate { get; set; }
    }
}
