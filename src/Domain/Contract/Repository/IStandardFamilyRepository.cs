using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository
{
    public interface IStandardFamilyRepository:IScopedDependency,IRepository<StandardFamily,StandardFamilyId,string>
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
        /// 查询所有标准族
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<StandardFamily?>> GetAllStandardFamilyAsync(CancellationToken ct);

        /// <summary>
        /// 通过标准Id获取标准族
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<StandardFamily> GetByStandardIdAsync(StandardId id, CancellationToken ct);

    }
}
