using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class ParamRuleRepository : IParamRuleRepository, IScopedDependency
    {
        public Task<IEnumerable<ParamRule>> GetByIdsAsync(IEnumerable<ParamRuleId> ids,CancellationToken ct)
        {
            return null;
        }

        public Task<IEnumerable<ParamRule>> GetByFormulaIdAsync(FormulaId formulaId)
        {
            return null;
        }

        public async Task AddAsync(ParamRule rule, CancellationToken ct) 
        {

        }

        public async Task UpdateAsync(ParamRule rule, CancellationToken ct) 
        { 

        }

        public async Task<ParamRule> GetByIdAsync(ParamRuleId id,CancellationToken ct) 
        {
            
            return null;
        }
    }
}
