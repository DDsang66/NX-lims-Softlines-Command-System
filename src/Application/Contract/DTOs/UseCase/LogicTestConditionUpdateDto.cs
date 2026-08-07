namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.UseCase
{
    public record LogicTestConditionUpdateDto
    {
        /// <summary>
        /// ConditionPool标识
        /// </summary>
        public Guid ConditionPoolId { get; set; }

        /// <summary>
        /// 更新的条件列表，键为条件名称，值为条件值
        /// </summary>
        public Dictionary<string, object?> Conditions { get; set; } = new Dictionary<string, object?>();
    }
}
