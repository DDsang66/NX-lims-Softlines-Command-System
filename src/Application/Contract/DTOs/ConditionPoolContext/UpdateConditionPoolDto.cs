namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ConditionPoolContext
{
    public record UpdateConditionPoolDto
    {
        /// <summary>
        /// ConditionPool标识
        /// </summary>
        public Guid ConditionPoolId { get; set; }

        /// <summary>
        /// 关联的测试清单ID
        /// </summary>
        public Guid CheckListId { get; set; } = Guid.Empty;

        /// <summary>
        /// 关联的订单ID
        /// </summary>
        public Guid OrderId { get; set; } = Guid.Empty;

        /// <summary>
        /// 测点列表
        /// </summary>
        public List<string> TestPoints { get; init; } = new();

        public Dictionary<string, object?> Conditions { get; set; }=new Dictionary<string, object?>();
    }
}
