using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext
{
    public interface IParamStructureRepository: IRepository<ParamStructure>, IScopedDependency
    {
        ParamStructure? GetById(ParamStructureId id);
        List<ParamStructure> GetByFamilyId(string standardFamilyId);
        List<ParamStructure> GetByParamName(string paramName);
        Task AddAsync(ParamStructure paramStructure);
        Task UpdateAsync(ParamStructure paramStructure);
    }
}
