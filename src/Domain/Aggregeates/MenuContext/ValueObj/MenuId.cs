using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MenuContext.ValueObj
{
    public class MenuId : AggregateRootId<string>
    {
        public MenuId(string value)
            : base(value)
        {
            if (value == string.Empty)
                throw new ArgumentNullException("MenuId cannot be empty", nameof(value));
        }
    }
}
