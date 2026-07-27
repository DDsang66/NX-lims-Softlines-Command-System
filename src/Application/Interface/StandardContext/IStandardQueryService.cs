using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.StandardContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.StandardContext
{
    public interface IStandardQueryService:IScopedDependency
    {
        /// <summary>
        /// 查询单条标准
        /// </summary>
        /// <returns></returns>
        Task<Result<StandardResponseDto>> GetStandardAsync(string id, CancellationToken ct);
        
        /// <summary>
        /// 查询所有标准
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<List<StandardResponseDto>>> GetStandardsAsync(CancellationToken ct);

        /// <summary>
        /// 根据条件查询标准
        /// </summary>
        /// <param name="QueryCondition"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<List<StandardResponseDto>>> GetStandardByCodeAsync(StandardQueryConditionDto queryCondition, CancellationToken ct);

    }
}
