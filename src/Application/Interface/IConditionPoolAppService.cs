using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface
{
    public interface IConditionPoolAppService:IScopedDependency
    {
        /// <summary>
        /// 新建草稿状态条件池
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> AddConditionPoolAsync(AddConditionPoolDto dto, CancellationToken ct);

        /// <summary>
        /// 回收前端的输入，更新条件池
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> UpdateConditionPoolAsync(UpdateConditionPoolDto dto, CancellationToken ct);

        /// <summary>
        /// 分组条件池
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> GroupConditionPoolAsync(List<UpdateConditionPoolDto> dto, CancellationToken ct);

        /// <summary>
        /// 获取条件池
        /// </summary>
        /// <param name="conditionPoolId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<ConditionPoolResponseDto>> GetConditionPoolAsync(Guid conditionPoolId, CancellationToken ct);
    }
}
