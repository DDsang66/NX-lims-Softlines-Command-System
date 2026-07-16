using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj
{
    public class CheckListId: AggregateRootId<Guid>
    {
        public CheckListId(Guid value)
            :base(value) 
        {
            if (value == Guid.Empty) 
                throw new ArgumentNullException("CheckListId cannot be empty", nameof(value));
 
        }
    }
}
