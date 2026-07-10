using Mapster;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class ParamStructureRepository:IParamStructureRepository,IScopedDependency
    {
        private readonly dbContext _dbContext;

        public ParamStructureRepository(dbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 查询结构
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<ParamStructure> GetByIdAsync(ParamStructureId id, CancellationToken ct) 
        {
            return null;
        }

        /// <summary>
        /// 根据标准族查询结构
        /// </summary>
        /// <param name="standardFamilyId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ParamStructure>> GetByFamilyIdAsync(StandardFamilyId standardFamilyId, CancellationToken ct)
        {
            return null;
        }

        /// <summary>
        /// 根据参数名称查询结构
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ParamStructure>> GetByParamName(string paramName, CancellationToken ct)
        {
            return null;
        }

        /// <summary>
        /// 添加参数结构
        /// </summary>
        /// <param name="paramStructure"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task AddAsync(ParamStructure paramStructure, CancellationToken ct) 
        {
            var paramStructurePo = paramStructure.Adapt<BasicParamStructure>();

           await  _dbContext.AddAsync(paramStructurePo, ct);

            await Task.CompletedTask;
        }

        /// <summary>
        /// 更新参数结构
        /// </summary>
        /// <param name="paramStructure"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task UpdateAsync(ParamStructure paramStructure, CancellationToken ct) 
        {
            await Task.CompletedTask;
        }

    }
}
