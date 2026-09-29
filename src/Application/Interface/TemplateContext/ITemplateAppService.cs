using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TemplateContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.TemplateContext
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

        /// <summary>
        /// update an existing template asynchronously.
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> UpdateTemplateAsync(UpdateTemplateDto dto, CancellationToken ct);

        /// <summary>
        /// publish an existing template asynchronously.
        /// </summary>
        /// <param name="templateId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result> TemplatePublishAsync(string templateId, CancellationToken ct);
    }
}
