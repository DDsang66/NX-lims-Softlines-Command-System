namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext
{
    public record AddCheckListDto
    {
        /// <summary>
        /// 关联的订单ID（ReportNumber）
        /// </summary>
        public string? SourceId { get; set; }

        /// <summary>
        /// 测试清单中的测试项
        /// </summary>
        public IReadOnlyList<CheckListItemDto> Items { get; set; }= new List<CheckListItemDto>();

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; } = string.Empty;

    }



    public record CheckListItemDto 
    {
        /// <summary>
        /// 测试项目ID
        /// </summary>
        public string TestItemId { get; set; } = string.Empty;

        /// <summary>
        /// 买家自定义测试项目ID
        /// </summary>
        public string? BuyerModifiedTestItemId { get; set; } = null;

        /// <summary>
        /// 标准ID
        /// </summary>
        public IEnumerable<string> StandardIds { get; set; } = Enumerable.Empty<string>();

        /// <summary>
        /// 买家自定义测试方法ID
        /// </summary>
        public string? BuyerModifiedTextMethodId { get; set; } = null;

        /// <summary>
        /// 测试小组
        /// </summary>
        public int TestGroup { get; set; }

        /// <summary>
        /// 样品列表
        /// </summary>
        public List<string> Samples { get; set; } = new();

        /// <summary>
        /// 买家限值
        /// </summary>
        public string Requirement { get; set; } = string.Empty;
    }
}
