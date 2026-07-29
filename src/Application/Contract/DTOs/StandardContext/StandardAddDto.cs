namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardContext
{
    public record StandardAddDto
    {
        /// <summary>
        /// 标准ID
        /// </summary>
        public string StandardId { get; set; } = string.Empty;

        /// <summary>
        /// 标准代码
        /// </summary>
        public string StandardCode { get; set; } = string.Empty;

        /// <summary>
        /// 标准名称（中文）
        /// </summary>
        public string StandardNameCn { get; set; } = string.Empty;

        /// <summary>
        /// 标准名称（英文）
        /// </summary>
        public string StandardNameEn { get; set; } = string.Empty;

        /// <summary>
        /// 标准族代码
        /// </summary>
        public string? StandardFamilyCode { get; set; }

        /// <summary>
        /// 状态：Draft, Active, Deprecated, Superseded, Pending
        /// </summary>
        public string Status { get; set; } = "Draft";
    }
}
