using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamFormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.FormulaContext
{
    public interface IFormulaQueryService:IScopedDependency
    {
        /// <summary>
        /// 根据id查询公式
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<FormulaResponseDto>> GetFormulaByIdAsync(string id, CancellationToken ct);

        /// <summary>
        /// 根据ids查询公式
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<List<FormulaResponseDto>>> GetFormulasByIdsAsync(IEnumerable<string> ids, CancellationToken ct);

        /// <summary>
        /// 查询所有公式
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<List<FormulaResponseDto>>> GetAllFormulaAsync(CancellationToken ct);
    }
}
