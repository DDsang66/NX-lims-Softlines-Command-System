using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext
{
    public class CheckListItem : Entity
    {
        /// <summary>
        /// 测试项标识
        /// 已继承实体基类，无需重复定义
        /// </summary>
        //public Guid Id { get; set; } = Guid.NewGuid();

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
        /// 测试小组
        /// </summary>
        public TestGroup TestGroup { get; set; } = new();

        /// <summary>
        /// 参数集
        /// </summary>
        public ParamSet? Param { get; set; } = new();

        /// <summary>
        /// 样品列表
        /// </summary>
        public List<string> Samples { get; set; } = new();

        /// <summary>
        /// 项目状态
        /// </summary>
        public CheckListStatus Status { get; set; } = CheckListStatus.Created;
       
        /// <summary>
        /// 测试清单ID
        /// </summary>
        public CheckListId CheckListId { get; set; } = new CheckListId(Guid.NewGuid());
    }
}
