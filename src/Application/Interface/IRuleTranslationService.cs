using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface
{
    public interface IRuleTranslationService:IScopedDependency
    {
        ConditionPattern PatternTranslateFromDto(CreateParamRuleRequest request,CancellationToken ct);
        (ConditionPattern pattern, ParamValue paramValue) ParseFromNaturalLanguageText(string text, Formula formula,CancellationToken ct);
    }
}
