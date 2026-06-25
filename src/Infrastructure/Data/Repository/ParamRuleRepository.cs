using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class ParamRuleRepository : IParamRuleRepository, IScopedDependency
    {
        public List<ParamRule> GetByIds(IEnumerable<ParamRuleId> ids)
        {
            return new List<ParamRule>();
        }

        public List<ParamRule> GetByFormulaId(FormulaId formulaId)
        {
            return new List<ParamRule>();
        }

        public async Task AddAsync(ParamRule rule) 
        {

        }

        public async Task UpdateAsync(ParamRule rule) 
        { 

        }

        public async Task<ParamRule> FindAsync(ParamRuleId id) 
        {
            
            return null;
        }
    }
}
