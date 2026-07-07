using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.NX_lims_Softlines_Command_System.src.Application.ParamEngineContext.Dtos;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface
{
    public interface IParamRuleApplicationService : IScopedDependency
    {
        Task<ParamRuleDto> CreateParamRuleAsync(CreateParamRuleRequest request, CancellationToken ct);
        Task<ParamRuleDto> UpdateParamRuleAsync(UpdateParamRuleRequest request, CancellationToken ct);
        Task<ParamRuleDto> GetParamRuleAsync(string id,CancellationToken ct);
    }
}
