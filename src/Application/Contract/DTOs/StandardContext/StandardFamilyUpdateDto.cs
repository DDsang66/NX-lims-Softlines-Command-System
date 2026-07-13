using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;

namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardContext
{
    public record StandardFamilyUpdateDto
    {
        /// <summary>
        /// 标准族ID
        /// </summary>
        public string StandardFamilyId { get; set; } = string.Empty;

        /// <summary>
        /// 标准族名称
        /// </summary>
        public string StandardFamilyCode { get; set; } = string.Empty;

        /// <summary>
        /// 持有标准id
        /// </summary>
        public IEnumerable<string> StandardIds { get; set; }  = Enumerable.Empty<string>();

        /// <summary>
        /// 持有公式
        /// </summary>
        public IEnumerable<string> FormulaIds { get; set; } = Enumerable.Empty<string>();

        /// <summary>
        /// 持有结构id
        /// </summary>
        public IEnumerable<string> ParamStructureIds { get; set; } = Enumerable.Empty<string>();

        /// <summary>
        /// 持有规则id
        /// </summary>
        public IEnumerable<string> ParamRuleIds { get; set; } = Enumerable.Empty<string>();
    }
}
