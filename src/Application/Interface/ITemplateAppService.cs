using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TemplateContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface
{
    public interface ITemplateAppService:IScopedDependency
    {
        /// <summary>
        /// Create a new template asynchronously.
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> CreateTemplateAsync(AddTemplateDto dto, CancellationToken ct);
    }
}
