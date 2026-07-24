using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface
{
    public interface ICheckListAppService:IScopedDependency
    {
        /// <summary>
        /// 添加清单
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> AddCheckList(AddCheckListDto dto, CancellationToken ct);

        /// <summary>
        /// 更新清单
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> UpdateCheckList(UpdateCheckListDto dto, CancellationToken ct);

        /// <summary>
        /// 获取清单
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<CheckListResponseDto>> GetCheckListAsync(Guid checkListId, CancellationToken ct);

        /// <summary>
        /// 获取清单列表
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> CalculateParamAsync(Guid id, CancellationToken ct);
    }
}
