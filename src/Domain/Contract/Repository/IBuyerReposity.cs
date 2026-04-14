using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository
{
    public interface IBuyerReposity:IScopedDependency
    {
        public Task<List<BasicBuyer>> GetBuyerListAsync(CancellationToken ct);
    }
}
