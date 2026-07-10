using Mapster;
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
            var standardFamilyPo = standardFamily.Adapt<BasicStandardFamily>();

            await _dbContext.AddAsync(standardFamilyPo, ct);

            await Task.CompletedTask;
        }

        /// <summary>
        /// 更新标准族
        /// </summary>
        /// <param name="standardFamily"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task UpdateAsync(StandardFamily standardFamily, CancellationToken ct)
        {
            var standardFamilyPo = await _dbContext.FindAsync<BasicStandardFamily>(standardFamily.Id.Value, ct);

            if (standardFamilyPo == null)
                throw new Exception($"标准族 {standardFamily.Id.Value} 不存在");

            standardFamilyPo.Adapt(standardFamily);

            await Task.CompletedTask;
        }

        /// <summary>
        /// 移除标准族
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task RemoveAsync(StandardFamilyId id, CancellationToken ct)
        {
            var standardFamilyPo = new BasicStandardFamily { IdStandardFamily = id.Value };

            _dbContext.Attach(standardFamilyPo);

            _dbContext.Remove(standardFamilyPo);

            await Task.CompletedTask;
        }

        /// <summary>
        /// 根据id查询标准族
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<StandardFamily?> GetByIdAsync(StandardFamilyId id, CancellationToken ct)
        {
            var standardFamilyPo = await  _dbContext.FindAsync<BasicStandardFamily>(id.Value,ct);

            if (standardFamilyPo == null)
                throw new Exception($"标准族 {id.Value} 不存在");

            return standardFamilyPo.Adapt<StandardFamily>();
        }
    }
}
