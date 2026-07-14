using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj
{
    public class CheckListId: AggregateRootId
    {
        public Guid Value { get; private set; }

        public CheckListId(Guid value)
        {
            if (value == Guid.Empty) throw new ArgumentException("CheckListId cannot be empty", nameof(value));
            Value = value;
        }

        public override string ToString() => Value.ToString();
    }
}
