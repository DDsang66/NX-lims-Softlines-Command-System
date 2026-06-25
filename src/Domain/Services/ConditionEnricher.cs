using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition;

namespace NX_lims_Softlines_Command_System.src.Domain.Services
{
    public class ConditionEnricher: IConditionEnricher
    {
        public ConditionPool Enrich(IDictionary<string, object?> rawData) 
        {
            return null;
        }
    }
}
