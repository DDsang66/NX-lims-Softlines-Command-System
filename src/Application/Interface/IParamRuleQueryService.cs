using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface
{
    public interface IParamRuleQueryService:IScopedDependency
    {
        /// <summary>
        /// 查询规则
        /// </summary>
        /// <param name="standardId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Dictionary<ParamStructureId, List<ParamRule>>> GetRulesByStandardAsync(StandardId standardId, CancellationToken ct);

        /// <summary>
        /// 获取参数规则
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<ParamRuleResponseDto>> GetByIdAsync(string id, CancellationToken ct);

        /// <summary>
        /// 获取所有规则
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<List<ParamRuleResponseDto>>> GetAllRulesAsync(CancellationToken ct);

        /// <summary>
        /// 根据公式id获取规则
        /// </summary>
        /// <param name="formulaId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<List<ParamRuleResponseDto>>> GetRulesByFormulaIdAsync(string formulaId, CancellationToken ct);
    }
}
