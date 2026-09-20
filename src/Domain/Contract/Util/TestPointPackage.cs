using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Util
{
    public class TestPointPackage
    {
        /// <summary>
        /// 测试点ID列表
        /// </summary>
        public IReadOnlyList<string> TestPointIds { get; }

        /// <summary>
        /// 参数集
        /// </summary>
        public ParamSet? ParamSet { get; }

        public TestPointPackage(
            IReadOnlyList<string> TestPointIds,   // ← 大写，和属性同名
            ParamSet? ParamSet)
        {
            this.TestPointIds = TestPointIds;
            this.ParamSet = ParamSet;
        }
    }
}
