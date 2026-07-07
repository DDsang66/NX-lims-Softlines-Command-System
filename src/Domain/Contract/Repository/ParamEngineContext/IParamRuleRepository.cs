using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext
{
    public interface IParamRuleRepository: IRepository<ParamRule>, IScopedDependency
    {
       Task<IEnumerable<ParamRule>> GetByFormulaIdAsync(FormulaId formulaId);
        Task<ParamRule> GetByIdAsync(ParamRuleId id,CancellationToken ct);
        Task<IEnumerable<ParamRule>> GetByIdsAsync(IEnumerable<ParamRuleId> ids, CancellationToken ct);
        Task AddAsync(ParamRule rule,CancellationToken ct);
        Task UpdateAsync(ParamRule rule,CancellationToken ct);
    }
}
