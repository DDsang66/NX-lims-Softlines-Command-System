using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;

namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardContext
{
    public record StandardResponseDto
    {
        /// <summary>
        /// 标准聚合根的Id，用于初始化标准的基本信息
        /// </summary>
        public string StandardId { get; set; } = null!;

        public string StandardCode { get; set; } = null!;

        public string? StandardCodeNameEn { get; set; } = null!;

        public string? StandardCodeNameChn { get; set; } = null!;

        public string Status { get; set; } = null!;

        public string? StandardFamilyCode { get; set; } = null!;

    }
}
