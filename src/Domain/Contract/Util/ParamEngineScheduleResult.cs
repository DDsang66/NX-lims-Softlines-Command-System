using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Util
{
    public sealed class ParamEngineScheduleResult
    {
        public IEnumerable<Formula> Formulas { get; init; } = Array.Empty<Formula>();
        public IEnumerable<ParamStructure> ParamStructures { get; init; } = Array.Empty<ParamStructure>();
        public IEnumerable<ParamRule> Rules { get; init; } = Array.Empty<ParamRule>();
    }
}
