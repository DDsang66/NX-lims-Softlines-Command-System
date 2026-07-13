using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.NX_lims_Softlines_Command_System.src.Application.ParamEngineContext.Dtos;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface
{
    public interface IParamRuleApplicationService : IScopedDependency
    {
        /// <summary>
        /// 添加json格式的参数规则
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> AddParamRuleFromJsonAsync(CreateParamRuleRequest request, CancellationToken ct);
        
        /// <summary>
        /// 添加自然语言格式的参数规则
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> AddParamRuleFromNaturalTextAsync(NaturalLanguageRuleRequest request, CancellationToken ct);
        
        /// <summary>
        /// 更新参数规则
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> UpdateParamRuleAsync(UpdateParamRuleRequest request, CancellationToken ct);
        
        /// <summary>
        /// 获取参数规则
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> GetParamRuleAsync(string id,CancellationToken ct);
    }
}
