namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ConditionPoolContext
{
    public record AddConditionPoolDto
    {
        /// <summary>
        /// 关联的测试清单ID
        /// </summary>
        public Guid CheckListId { get; set; }=Guid.Empty;

        /// <summary>
        /// 关联的订单ID
        /// </summary>
        public string OrderId { get; set; } = string.Empty;
    }
}
