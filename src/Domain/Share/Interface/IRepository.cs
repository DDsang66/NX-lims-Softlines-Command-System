using NX_lims_Softlines_Command_System.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Share.Interface
{
    /// <summary>
    /// 仓储接口
    /// </summary>
    /// <typeparam name="T">聚合根标记接口</typeparam>
    /// <typeparam name="TId">聚合根唯一标识标记接口</typeparam>
    public interface IRepository<T, TId,TValue> 
        where T: IAggregateRoot<TId, TValue> 
        where TId: IAggregateRootId<TValue>
        where TValue : notnull
    {
        /// <summary>
        /// 添加聚合根
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <returns></returns>
        Task AddAsync(T aggregateRoot,CancellationToken ct);

        /// <summary>
        /// 修改聚合根
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <returns></returns>
        Task UpdateAsync(T aggregateRoot,CancellationToken ct);

        /// <summary>
        /// 查询聚合根
        /// </summary>
        /// <param name="aggregateRootId"></param>
        /// <param name="ct"></param>
        /// <returns>聚合根</returns>
        Task <T> GetByIdAsync(TId aggregateRootId, CancellationToken ct);
    }
}
