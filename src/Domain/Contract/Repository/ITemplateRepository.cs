using DocumentFormat.OpenXml.ExtendedProperties;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;
using Template = NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext.Template;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository
{
    public interface ITemplateRepository:IRepository<Template,TemplateId,string>,IScopedDependency
    {
        /// <summary>
        /// 添加聚合根
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <returns></returns>
        Task AddAsync(Template aggregateRoot, CancellationToken ct);

        /// <summary>
        /// 修改聚合根
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <returns></returns>
        Task UpdateAsync(Template aggregateRoot, CancellationToken ct);

        /// <summary>
        /// 查询聚合根
        /// </summary>
        /// <param name="aggregateRootId"></param>
        /// <param name="ct"></param>
        /// <returns>聚合根</returns>
        Task<Template> GetByIdAsync(TemplateId aggregateRootId, CancellationToken ct);
    }
}
