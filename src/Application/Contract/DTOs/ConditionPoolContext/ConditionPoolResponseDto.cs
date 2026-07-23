using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.Enums;

namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ConditionPoolContext
{
    public record ConditionPoolResponseDto
    {
        /// <summary>
        /// ConditionPool标识
        /// </summary>
        public Guid ConditionPoolId { get; set; }

        /// <summary>
        /// 条件池
        /// </summary>
        public Dictionary<string, object?> Conditions { get; set; } = new Dictionary<string, object?>();

        /// <summary>
        /// 条件池的创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 条件池的状态
        /// </summary>
        public string Status { get; set; } = "Draft"; // Draft, Validated, Expired
    }
}
