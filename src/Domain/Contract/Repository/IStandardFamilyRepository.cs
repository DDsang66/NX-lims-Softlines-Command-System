using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository
{
    public interface IStandardFamilyRepository:IScopedDependency,IRepository<StandardFamily>
    {
        /// <summary>
        /// 添加标准族
        /// </summary>
        /// <param name="standard"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task AddAsync(StandardFamily standardFamily, CancellationToken ct);

        /// <summary>
        /// 更新标准族
        /// </summary>
        /// <param name="standard"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UpdateAsync(StandardFamily standardFamily, CancellationToken ct);

        /// <summary>
        /// 移除标准族
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task RemoveAsync(StandardFamilyId id, CancellationToken ct);

        /// <summary>
        /// 获取标准族
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<StandardFamily> GetByIdAsync(StandardFamilyId id, CancellationToken ct);

        /// <summary>
        /// 获取标准族列表
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<List<StandardFamily>> GetStandardListAsync(CancellationToken ct);
    }
}
