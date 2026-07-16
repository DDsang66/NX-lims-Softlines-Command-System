using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj
{
    public class StandardId : AggregateRootId<string>
    {
        public StandardId(string value)
            :base(value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));

            if (value.Length > 50)
                throw new ArgumentException("IdStandard cannot exceed 50 characters.", nameof(value));
        }
    }
}
