using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Report.ValueObj
{
    public class TestReportId : AggregateRootId<Guid>
    {
        public TestReportId(Guid value)
            : base(value)
        {
            if (value == Guid.Empty)
                throw new ArgumentNullException("TestReportId cannot be empty", nameof(value));

        }
    }
}
