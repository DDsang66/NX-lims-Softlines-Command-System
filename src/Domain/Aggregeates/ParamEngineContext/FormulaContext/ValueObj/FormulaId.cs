using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj
{
    public class FormulaId:AggregateRootId<string>
    {
        public FormulaId(string value)
            :base(value)
        {
            if (string.IsNullOrWhiteSpace(value)) 
                throw new ArgumentNullException("FormulaId required", nameof(value));

        }
    }
}
