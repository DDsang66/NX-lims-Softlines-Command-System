using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.ParamStructureContext
{
    public interface IParamStructureQueryService:IScopedDependency
    {
        /// <summary>
        /// 获取参数结构
        /// </summary>
        /// <param name="paramStructureId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<ParamStructureResponseDto>> GetParamStructureAsync(string paramStructureId, CancellationToken ct);

        /// <summary>
        /// 获取参数结构列表
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<List<ParamStructureResponseDto>>> GetAllStructureAsync( CancellationToken ct);

        /// <summary>
        /// 根据标准族获取参数结构
        /// </summary>
        /// <param name="familyId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<List<ParamStructureResponseDto>>> GetByFamilyIdAsync(string familyId, CancellationToken ct);

        /// <summary>
        /// 根据参数名称获取参数结构
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<List<ParamStructureResponseDto>>> GetByParamNameAsync(string paramName, CancellationToken ct);
    }
}
