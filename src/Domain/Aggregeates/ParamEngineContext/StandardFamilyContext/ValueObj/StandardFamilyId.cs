using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj
{
    public class StandardFamilyId:AggregateRootId<string>
    {
        public StandardFamilyId(string value)
            :base(value) 
        {
            if (string.IsNullOrWhiteSpace(value)) 
                throw new ArgumentNullException("StandardFamilyId is required", nameof(value));
        }
    }
}
