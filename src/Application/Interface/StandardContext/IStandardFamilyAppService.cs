using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.StandardContext
{
    public interface IStandardFamilyAppService: IScopedDependency
    {
        /// <summary>
        /// 添加标准族
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> AddStandardFamilyAsync(StandardFamilyAddDto dto, CancellationToken ct);

        /// <summary>
        /// 更新标准族自有字段
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> UpdateStandardFamilyAsync(StandardFamilyUpdateDto dto, CancellationToken ct);

        /// <summary>
        /// 移除标准族
        /// </summary>
        /// <returns></returns>
        Task<Result> RemoveStandardFamilyAsync(string id, CancellationToken ct);

        /// <summary>
        /// 向标准族添加标准
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> AddStandardToFamilyAsync(AddStandardToFamilyDto dto, CancellationToken ct);

        /// <summary>
        /// 向标准族添加公式
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> AddFormulaToFamilyAsync(AddFormulaToFamilyDto dto, CancellationToken ct);


        Task<Result> AddStructureToFamilyAsync(StandardFamilyUpdateDto dto, CancellationToken ct);


        Task<Result> AddRuleToFamilyAsync(StandardFamilyUpdateDto dto, CancellationToken ct);
    }
}
