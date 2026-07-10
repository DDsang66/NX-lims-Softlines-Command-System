using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext
{
    public interface IParamStructureRepository: IRepository<ParamStructure, ParamStructureId>, IScopedDependency
    {
        /// <summary>
        /// 查询结构
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ParamStructure> GetByIdAsync(ParamStructureId id, CancellationToken ct);

        /// <summary>
        /// 根据标准族查询结构
        /// </summary>
        /// <param name="standardFamilyId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<ParamStructure>> GetByFamilyIdAsync(StandardFamilyId standardFamilyId,CancellationToken ct);

        /// <summary>
        /// 根据参数名称查询结构
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<ParamStructure>> GetByParamName(string paramName, CancellationToken ct);

        /// <summary>
        /// 添加参数结构
        /// </summary>
        /// <param name="paramStructure"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task AddAsync(ParamStructure paramStructure,CancellationToken ct);

        /// <summary>
        /// 更新参数结构
        /// </summary>
        /// <param name="paramStructure"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UpdateAsync(ParamStructure paramStructure,CancellationToken ct);
    }
}
