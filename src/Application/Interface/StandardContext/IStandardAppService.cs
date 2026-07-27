using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.StandardContext
{
    public interface IStandardAppService:IScopedDependency
    {
        /// <summary>
        /// 新增标准
        /// </summary>
        /// <returns></returns>
        Task<Result> AddStandardAsync(StandardAddDto dto, CancellationToken ct);

        /// <summary>
        /// 移除标准
        /// </summary>
        /// <returns></returns>
        Task<Result> RemoveStandardAsync(string id, CancellationToken ct);

        /// <summary>
        /// 更新标准信息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> UpdateStandardAsync(StandardUpdateDto dto, CancellationToken ct);

        /// <summary>
        /// 激活标准
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> ActiveStandardAsync(string id, CancellationToken ct);


        /// <summary>
        /// 将标准转变为草稿
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> DraftStandardAsync(string id, CancellationToken ct);

        /// <summary>
        /// 将标准转变为草稿
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> DeprecatedStandardAsync(string id, CancellationToken ct);
    }
}
