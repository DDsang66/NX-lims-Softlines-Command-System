using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service
{
    public interface ITemplateValidator:IScopedDependency
    {
        /// <summary>
        /// Validates template
        /// </summary>
        /// <param name="template"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ValidationResult> ValidateAsync(Template template, CancellationToken ct);
    }
}
