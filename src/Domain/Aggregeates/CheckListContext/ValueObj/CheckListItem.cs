using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj
{
    public class CheckListItem
    {
        /// <summary>
        /// 测试项目ID
        /// </summary>
        public string TestItemId { get; set; } = string.Empty;

        /// <summary>
        /// 买家自定义测试项目ID
        /// </summary>
        public string BuyerModifiedTestItemId { get; set; } = string.Empty;

        /// <summary>
        /// 标准ID
        /// </summary>
        public StandardId StandardId { get; set; } = new StandardId(string.Empty);

        /// <summary>
        /// 买家自定义测试方法ID
        /// </summary>
        public string BuyerModifiedTextMethodId { get; set; } = string.Empty;

        /// <summary>
        /// 参数集
        /// </summary>
        public ParamSet Param { get; set; } = new();

        /// <summary>
        /// 样品列表
        /// </summary>
        public List<string> Samples { get; set; } = new();
    }
}
