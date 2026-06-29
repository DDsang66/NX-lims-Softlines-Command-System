using NX_lims_Softlines_Command_System.Domain.Aggregeates.Standard;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class StandardRepository : IStandardRepository, IScopedDependency
    {
        // TODO: 领域聚合根 Standard(string IdStandard) 与 EF 实体 Standard(int StandardId)
        // 结构不兼容，暂用占位实现，待团队统一后补全 DB 映射
        public async Task AddAsync(Standard standard, CancellationToken ct)
        {
            await Task.CompletedTask;
        }

        public async Task UpdateAsync(Standard standard, CancellationToken ct)
        {
            await Task.CompletedTask;
        }

        public async Task RemoveAsync(StandardId id, CancellationToken ct) 
        {
            await Task.CompletedTask;
        }

        public async Task<Standard?> GetByIdAsync(StandardId id, CancellationToken ct)
        {
            return null;
        }

        public async Task<List<Standard>> GetStandardListAsync(CancellationToken ct) 
        {
            return new List<Standard>();
        }


    }
}

