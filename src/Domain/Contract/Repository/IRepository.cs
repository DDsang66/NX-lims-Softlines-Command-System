using NX_lims_Softlines_Command_System.Domain.Shared.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository
{
    /// <summary>
    /// 仓储接口
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRepository<T> where T: IAggregateRoot
    {
        /// <summary>
        /// 添加实体
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <returns></returns>
        Task AddAsync(T aggregateRoot);

        /// <summary>
        /// 修改实体
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <returns></returns>
        Task UpdateAsync(T aggregateRoot);

    }
}
