using Mapster;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class TestItemRepository: ITestItemRepository, IScopedDependency
    {
        private readonly dbContext _dbContext;

        public TestItemRepository(dbContext dbContext) 
        {
            _dbContext = dbContext;
        }
        /// <summary>
        /// 添加聚合根
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <returns></returns>
        public async Task AddAsync(TestItem aggregateRoot, CancellationToken ct) { }

        /// <summary>
        /// 修改聚合根
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <returns></returns>
        public async Task UpdateAsync(TestItem aggregateRoot, CancellationToken ct) { }

        /// <summary>
        /// 查询聚合根
        /// </summary>
        /// <param name="aggregateRootId"></param>
        /// <param name="ct"></param>
        /// <returns>聚合根</returns>
        public async Task<TestItem> GetByIdAsync(TestItemId aggregateRootId, CancellationToken ct) 
        {
            var testItemPo = await  _dbContext.FindAsync<BasicItem>(aggregateRootId.Value, ct);
           
            var testItem = testItemPo.Adapt<TestItem>();
            
            return testItem;
        }
    }
}
