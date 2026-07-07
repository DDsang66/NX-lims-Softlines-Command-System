using NX_lims_Softlines_Command_System.Domain.Aggregeates.Standard;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository
{
    public interface IStandardRepository:IScopedDependency,IRepository<Standard>
    {
        /// <summary>
        /// 添加标准
        /// </summary>
        /// <param name="standard"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task AddAsync(Standard standard,CancellationToken ct);

        /// <summary>
        /// 更新标准
        /// </summary>
        /// <param name="standard"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UpdateAsync(Standard standard, CancellationToken ct);

        /// <summary>
        /// 批量更新标准
        /// </summary>
        /// <param name="standard"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UpdateRangeAsync(IEnumerable<Standard> standards, CancellationToken ct);

        /// <summary>
        /// 移除标准
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RemoveAsync(StandardId id, CancellationToken ct);

        /// <summary>
        /// 获取标准列表
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Standard> GetByIdAsync(StandardId id, CancellationToken ct);

        /// <summary>
        /// 获取标准列表
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<Standard>> GetByIdsAsync(IEnumerable<StandardId> ids, CancellationToken ct);

        /// <summary>
        /// 获取标准列表
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<Standard>> GetStandardListAsync(CancellationToken ct);
    }
}
