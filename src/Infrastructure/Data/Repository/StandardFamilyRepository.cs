using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class StandardFamilyRepository:IStandardFamilyRepository, IScopedDependency
    {
        public async Task AddAsync(StandardFamily standardFamily, CancellationToken ct)
        {
            await Task.CompletedTask;
        }

        public async Task UpdateAsync(StandardFamily standardFamily, CancellationToken ct)
        {
            await Task.CompletedTask;
        }

        public async Task RemoveAsync(StandardFamilyId id, CancellationToken ct)
        {
            await Task.CompletedTask;
        }

        public async Task<StandardFamily?> GetByIdAsync(StandardFamilyId id, CancellationToken ct)
        {
            return null;
        }

        public async Task<List<StandardFamily>> GetStandardListAsync(CancellationToken ct)
        {
            return new List<StandardFamily>();
        }
    }
}
