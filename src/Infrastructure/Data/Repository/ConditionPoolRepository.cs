using Mapster;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class ConditionPoolRepository: IConditionPoolRepository, IScopedDependency
    {
        private readonly dbContext _dbContext;

        public ConditionPoolRepository(dbContext dbContext) 
        {
            _dbContext = dbContext;
        }
        /// <summary>
        /// 添加聚合根
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <returns></returns>
        public async Task AddAsync(Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ConditionPool aggregateRoot, CancellationToken ct) 
        {
            var conditionPoolPo = aggregateRoot.Adapt<src.Infrastructure.Data.Persistence.ConditionPool>();

           await  _dbContext.AddAsync(conditionPoolPo,ct);
        }

        /// <summary>
        /// 修改聚合根
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <returns></returns>
        public async Task UpdateAsync(Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ConditionPool aggregateRoot, CancellationToken ct) 
        {

        }

        /// <summary>
        /// 查询聚合根
        /// </summary>
        /// <param name="aggregateRootId"></param>
        /// <param name="ct"></param>
        /// <returns>聚合根</returns>
        public async Task<Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ConditionPool> GetByIdAsync(ConditionPoolId aggregateRootId, CancellationToken ct) 
        {
            return null;
        }

        /// <summary>
        /// 根据测试清单Id查询条件池
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ConditionPool> GetByCheckListIdAsync(CheckListId id, CancellationToken ct) 
        {
            return null;
        }
    }
}
