namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ConditionPoolContext
{
    public record UpdateConditionPoolDto
    {
        /// <summary>
        /// ConditionPool标识
        /// </summary>
        public Guid ConditionPoolId { get; set; }

        public Dictionary<string, object?> Conditions { get; set; }=new Dictionary<string, object?>();
    }
}
