using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.TemplateContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.TemplateContext
{
    public interface ITemplateQueryService:IScopedDependency
    {
        /// <summary>
        /// 获取所有模板
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Result<List<TemplateResponseDto>>> GetAllTemplateAsync(CancellationToken ct);
    }
}
