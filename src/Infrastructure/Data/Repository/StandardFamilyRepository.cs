using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class StandardFamilyRepository:IStandardFamilyRepository, IScopedDependency
    {
        private readonly dbContext _dbContext;

        public StandardFamilyRepository(dbContext dbContext) 
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Add new StandardFamily
        /// </summary>
        /// <param name="standardFamily"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task AddAsync(StandardFamily standardFamily, CancellationToken ct)
        {
            await _dbContext.AddAsync(standardFamily, ct);

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
