using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext
{
    public sealed class TestItem:AggregateRoot<TestItemId,string>
    {
        /// <summary>
        /// TestItemId
        /// </summary>
        //public TestItemId Id { get; private set; }
        public string NameEN { get; private set; } = string.Empty;
        public string NameChn { get; private set; } = string.Empty;

    }
}
