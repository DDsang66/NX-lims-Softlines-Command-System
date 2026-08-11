using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using Template = NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext.Template;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class TemplateRepository:ITemplateRepository,IScopedDependency
    {
        /// <summary>
        /// 添加聚合根
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <returns></returns>
        public async Task AddAsync(Template aggregateRoot, CancellationToken ct) 
        {

        }

        /// <summary>
        /// 修改聚合根
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <returns></returns>
        public async Task UpdateAsync(Template aggregateRoot, CancellationToken ct) 
        {

        }

        /// <summary>
        /// 查询聚合根
        /// </summary>
        /// <param name="aggregateRootId"></param>
        /// <param name="ct"></param>
        /// <returns>聚合根</returns>
        public async Task<Template> GetByIdAsync(TemplateId aggregateRootId, CancellationToken ct) 
        {
            return null;
        }
    }
}
