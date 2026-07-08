using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using System.Runtime.CompilerServices;

namespace NX_lims_Softlines_Command_System.src.Application.Interface
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
    }
}
