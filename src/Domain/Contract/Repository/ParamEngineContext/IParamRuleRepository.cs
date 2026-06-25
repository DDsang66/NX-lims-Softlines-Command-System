using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext
{
    public interface IParamRuleRepository: IRepository<ParamRule>, IScopedDependency
    {
        List<ParamRule> GetByIds(IEnumerable<ParamRuleId> ids);
        List<ParamRule> GetByFormulaId(FormulaId formulaId);
        Task AddAsync(ParamRule rule);
        Task UpdateAsync(ParamRule rule);
        Task<ParamRule> FindAsync(ParamRuleId id);
    }
}
