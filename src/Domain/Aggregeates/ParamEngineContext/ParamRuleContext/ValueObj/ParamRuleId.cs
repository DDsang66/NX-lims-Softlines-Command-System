using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj
{
    public class ParamRuleId:AggregateRootId<string>
    {
        public ParamRuleId(string value)
            :base(value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException("ParamRuleId required", nameof(value));
        }
    }
}
