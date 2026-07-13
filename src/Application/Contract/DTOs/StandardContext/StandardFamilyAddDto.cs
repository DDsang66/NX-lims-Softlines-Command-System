namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardContext
{
    public record StandardFamilyAddDto
    {
        /// <summary>
        /// 标准族ID
        /// </summary>
        public string StandardFamilyId { get; set; } = string.Empty;

        /// <summary>
        /// 标准族名称
        /// </summary>
        public string StandardFamilyCode { get; set; } = string.Empty;
    }
}
