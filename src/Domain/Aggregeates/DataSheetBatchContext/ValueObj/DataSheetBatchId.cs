using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetBatchContext.ValueObj
{
    public class DataSheetBatchId : AggregateRootId<Guid>
    {
        public DataSheetBatchId(Guid value)
            : base(value)
        {
            if (value == Guid.Empty)
                throw new ArgumentNullException("DataSheetBatchId cannot be empty", nameof(value));
        }
    }
}
 
