using DocumentFormat.OpenXml.Office2010.Excel;
using Mapster;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;
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
            var conditionPoolPo = aggregateRoot.Adapt<src.Infrastructure.Data.Persistence.ConditionPool>();

            _dbContext.Update(conditionPoolPo);
        }

        /// <summary>
        /// 删除聚合根
        /// </summary>
        /// <param name="aggregateRootId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task RemoveAsync(ConditionPoolId aggregateRootId, CancellationToken ct) 
        {
            var conditionPoolPo = await _dbContext.ConditionPools.FindAsync(aggregateRootId.Value, ct);

            if (conditionPoolPo is null)
                throw new InvalidOperationException($"未找到ID为 {aggregateRootId.Value} 的条件池，无法删除。");
            
            _dbContext.Attach(conditionPoolPo);

            _dbContext.Remove(conditionPoolPo);
        }

        /// <summary>
        /// 查询聚合根
        /// </summary>
        /// <param name="aggregateRootId"></param>
        /// <param name="ct"></param>
        /// <returns>聚合根</returns>
        public async Task<Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ConditionPool> GetByIdAsync(ConditionPoolId aggregateRootId, CancellationToken ct) 
        {
            // 1. 根据强类型 ID 提取底层值去数据库查询 PO
            var po = await _dbContext.ConditionPools
                .AsNoTracking()
                .OrderBy(p => p.CreatedAt)
                .FirstOrDefaultAsync(p => p.ConditionPoolId == aggregateRootId.Value, ct);

            // 2. 如果没查到，直接返回 null
            if (po is null)
                throw new InvalidOperationException("未找到条件池，无法查询。");

            // 自动处理 JSON 字符串到字典的反序列化
            return po.Adapt<Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ConditionPool>();
        }

        /// <summary>
        /// 根据测试清单Id查询条件池
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ConditionPool>> GetByCheckListIdAsync(CheckListId id, CancellationToken ct) 
        {
            // 1. 根据强类型 ID 提取底层值去数据库查询 PO
            var pos = await _dbContext.ConditionPools
                .AsNoTracking()
                .Where(c => c.CheckListId == id.Value)
                .ToListAsync(ct);
            // 2. 如果没查到，直接返回 null
            if (pos is null)
            {
                return null;
            }
            var conditionPools = new List<Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ConditionPool>();
            foreach (var p in pos) 
            {
                var conditionPool = p.Adapt<Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ConditionPool>();

                conditionPools.Add(conditionPool);
            }

            // 自动处理 JSON 字符串到字典的反序列化
            return conditionPools;
        }

        /// <summary>
        /// 根据检查单ID查询条件池
        /// </summary>
        /// <param name="checklistId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ConditionPool> GetOriginalPoolByCheckListIdAsync(CheckListId checkListId, CancellationToken ct)
        {
            var po = await _dbContext.ConditionPools
                .AsNoTracking()
                .OrderBy(p => p.CreatedAt)
                .FirstOrDefaultAsync(p => p.CheckListId == checkListId.Value, ct);

            // 2. 如果没查到，直接返回 null
            if (po is null)
                throw new InvalidOperationException("未找到条件池，无法查询。");

            // 自动处理 JSON 字符串到字典的反序列化
            return po.Adapt<Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ConditionPool>();
        }
    }
}
