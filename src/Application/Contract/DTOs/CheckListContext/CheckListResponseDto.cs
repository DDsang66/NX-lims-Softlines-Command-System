using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext
{
    public record CheckListResponseDto
    {
        public string ChecklistId { get; set; } = string.Empty;

        public IEnumerable<CheckListResponseItemDto> Items { get; set; } = Enumerable.Empty<CheckListResponseItemDto>();
    }

    public record CheckListResponseItemDto 
    {
        /// <summary>
        /// 测试项目ID
        /// </summary>
        public string TestItem { get; set; } = string.Empty;

        /// <summary>
        /// 标准ID
        /// </summary>
        public IEnumerable<string> Standards { get; set; } = Enumerable.Empty<string>();

        /// <summary>
        /// 测试小组
        /// </summary>
        public int TestGroup { get; set; }

        /// <summary>
        /// 样品列表
        /// </summary>
        public List<string> Samples { get; set; } = new();

        /// <summary>
        /// 测试参数
        /// </summary>
        public string Parameter { get; set; } = string.Empty;

        /// <summary>
        /// 买家限值
        /// </summary>
        public string Requirement { get; set; } = string.Empty;

        /// <summary>
        /// 取样方法
        /// </summary>
        public string CuttingMethod { get; set; } = string.Empty;
    }
}
