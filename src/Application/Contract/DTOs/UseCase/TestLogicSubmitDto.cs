namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.UseCase
{
    public record TestLogicSubmitDto
    {
        /// <summary>
        /// 条件池ID
        /// </summary>
        public Guid ConditionPoolId { get; init; }

        /// <summary>
        /// 需要验证的公式ID列表
        /// </summary>
        public List<string> FormulaIds { get; init; } = new List<string>();
    }
}
