using Microsoft.EntityFrameworkCore.ChangeTracking;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MenuContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MenuContext
{
    public class MenuItem:Entity
    {
        /// <summary>
        /// 测试项目ID
        /// </summary>
        public TestItemId? TestItemId { get; set; }

        /// <summary>
        /// 买家自定义测试项目ID
        /// </summary>
        public string? BuyerModifiedTestItemId { get; set; } = string.Empty;

        /// <summary>
        /// 标准ID
        /// </summary>
        public IEnumerable<StandardId?> StandardIds { get; set; } = Enumerable.Empty<StandardId>();

        /// <summary>
        /// 买家自定义测试方法ID
        /// </summary>
        public string? BuyerModifiedTextMethodId { get; set; } = string.Empty;

        /// <summary>
        /// 限值
        /// </summary>
        public string? BuyerModifiedGroup { get; set; } = string.Empty;

        /// <summary>
        /// 限值
        /// </summary>
        public string? Requirement { get; set; } = string.Empty;
    }
}
