using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamFormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using System.Runtime.CompilerServices;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.FormulaContext
{
    public interface IFormulaAppService : IScopedDependency
    {
        /// <summary>
        /// 添加公式
        /// </summary>
        /// <param name="formulaDto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> AddFormulaAsync(AddFormulaDto formulaDto,CancellationToken ct);

        /// <summary>
        /// 更新公式
        /// </summary>
        /// <param name="formulaDto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> UpdateFormulaAsync(UpdateFormulaDto formulaDto, CancellationToken ct);

        /// <summary>
        /// 删除公式
        /// </summary>
        /// <param name="formulaId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> DeleteFormulaAsync(string formulaId, CancellationToken ct);

        /// <summary>
        /// 激活公式
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> ActiveFormulaAsync(string id, CancellationToken ct);
    }
}
