using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.StandardContext
{
    public interface IStandardFamilyQueryService:IScopedDependency
    {
        /// <summary>
        /// 查询单条标准
        /// </summary>
        /// <returns></returns>
        Task<Result<StandaradFamilyResponseDto>> GetStandardFamilyAsync(string id, CancellationToken ct);


        /// <summary>
        /// 查询所有标准
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<List<StandaradFamilyResponseDto>>> GetStandardFamiliesAsync(CancellationToken ct);
    }
}
