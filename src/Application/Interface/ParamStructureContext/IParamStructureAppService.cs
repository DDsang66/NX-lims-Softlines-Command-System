using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.ParamStructureContext
{
    public interface IParamStructureAppService:IScopedDependency
    {
        /// <summary>
        /// 添加
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> AddParamStructureAsync(AddParamStructureDto dto, CancellationToken ct);

        /// <summary>
        /// 更新
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> UpdateParamStructureAsync(UpdateParamStructureDto dto, CancellationToken ct);

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="paramStructureId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> RemoveParamStructureAsync(string paramStructureId, CancellationToken ct);
    }
}
