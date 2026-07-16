using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj
{
    public class ParamStructureId : AggregateRootId<string>
    {
        public ParamStructureId(string value)
            :base(value)
        {
            if (string.IsNullOrWhiteSpace(value)) 
                throw new ArgumentNullException("ParamStructureId is required", nameof(value));
        }
    }
}
