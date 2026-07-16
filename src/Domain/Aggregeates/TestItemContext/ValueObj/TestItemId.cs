using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj
{
    public class TestItemId : AggregateRootId<string>
    {
        // 显式构造函数，接收 string 类型的值，并传给基类
        public TestItemId(string value) 
            : base(value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));

            if (value.Length > 50)
                throw new ArgumentOutOfRangeException("IdStandard cannot exceed 50 characters.", nameof(value));
        }
    }
}
