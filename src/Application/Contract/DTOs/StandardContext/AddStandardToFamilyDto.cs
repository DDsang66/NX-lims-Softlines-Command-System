namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardContext
{
    public record AddStandardToFamilyDto
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
        public IEnumerable<string> StandardIds { get; set; } = Enumerable.Empty<string>();
    }
}
