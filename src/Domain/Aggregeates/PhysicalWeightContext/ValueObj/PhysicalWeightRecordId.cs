using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.PhysicalWeightContext.ValueObj
{
    public class PhysicalWeightRecordId : AggregateRootId<Guid>
    {
        public PhysicalWeightRecordId(Guid value)
            : base(value)
        {
            if (value == Guid.Empty)
                throw new ArgumentException("PhysicalWeightRecordId cannot be empty", nameof(value));
        }
    }
}
