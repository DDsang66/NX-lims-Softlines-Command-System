using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext.ValueObj
{
    public class DataSheetId : AggregateRootId<Guid>
    {
        public DataSheetId(Guid value)
            : base(value)
        {
            if (value == Guid.Empty)
                throw new ArgumentNullException("DataSheetId cannot be empty", nameof(value));
        }
    }
}
