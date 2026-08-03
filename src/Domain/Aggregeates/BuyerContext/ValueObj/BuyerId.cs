using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.BuyerContext.ValueObj
{
    public class BuyerId:AggregateRootId<string>
    {
        public BuyerId(string value)
            : base(value)
        {
            if (value == string.Empty)
                throw new ArgumentNullException("BuyerId cannot be empty", nameof(value));
        }
    }
}
