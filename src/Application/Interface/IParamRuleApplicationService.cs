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
        /// 通过Json更新参数规则
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> UpdateParamRuleWithJsonAsync(UpdateParamRuleJsonRequest request, CancellationToken ct);

        /// <summary>
        /// 通过自然文本更新参数规则
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> UpdateParamRuleWithNaturalTextAsync(UpdateParamRuleTextRequest request, CancellationToken ct);

        /// <summary>
        /// 激活规则
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> ActiveParamRuleAsync(string id, CancellationToken ct);

        /// <summary>
        /// 禁用规则
        /// </summary>
        Task<Result> DeactiveParamRuleAsync(string id, CancellationToken ct);
    }
}
