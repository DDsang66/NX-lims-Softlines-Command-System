using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext
{
    public interface IFormulaRepository:IRepository<Formula>, IScopedDependency
    {
        Formula? GetById(FormulaId id);
        List<Formula> GetByFamilyId(string standardFamilyId);
        List<Formula> GetByParamName(string paramName);
        Task AddAsync(Formula formula);
        Task UpdateAsync(Formula formula);
    }
}
