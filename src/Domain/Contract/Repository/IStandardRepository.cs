using NX_lims_Softlines_Command_System.Domain.Aggregeates.Standard;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository
{
    public interface IStandardRepository:IScopedDependency
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
    }
}
