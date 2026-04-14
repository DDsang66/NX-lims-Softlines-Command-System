using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class BuyerRepository: IBuyerReposity,IScopedDependency
    {
        private readonly dbContext _context;
        public BuyerRepository(dbContext context)
        {
            _context = context;
        }
        /// <summary>
        /// 获取买家信息
        /// </summary>
        /// <returns></returns>
        public async Task<List<BasicBuyer>> GetBuyerListAsync(CancellationToken ct) 
        {
            var buyers = await _context.BasicBuyers.ToListAsync(ct);

            return buyers;
        }


    }
}
