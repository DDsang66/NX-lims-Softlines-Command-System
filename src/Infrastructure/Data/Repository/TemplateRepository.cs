using Mapster;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;
using Template = NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext.Template;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class TemplateRepository:ITemplateRepository,IScopedDependency
    {
        private readonly dbContext  _context;

        public TemplateRepository(dbContext context) 
        {
            _context = context;
        }
        /// <summary>
        /// 添加聚合根
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <returns></returns>
        public async Task AddAsync(Template aggregateRoot, CancellationToken ct) 
        {
            if (aggregateRoot == null)
            {
                throw new ArgumentNullException(nameof(aggregateRoot));
            }

            var templatePo = aggregateRoot.Adapt<src.Infrastructure.Data.Persistence.Template>();

            // 将聚合根添加到 DbContext 的内存集合中
            await  _context.Set<src.Infrastructure.Data.Persistence.Template>().AddAsync(templatePo, ct);

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

        /// <summary>
        /// 查询所有聚合根
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<List<Template>> GetAllAsync(CancellationToken ct)
        {
            // 1. 从数据库表（PO）中查询所有数据
            var templatePOs = await _context.Set<src.Infrastructure.Data.Persistence.Template>()
                .AsNoTracking() // 查询时不跟踪变更，提升性能
                .ToListAsync(ct);

            // 2. 将 PO 列表映射为聚合根列表
            var templates = templatePOs.Select(po => Template.Rebuild(
                id: new TemplateId(po.TemplateId),
                templateName: po.TemplateName,
                templateUrl: po.TemplateUrl,
                site: (Site)po.Site,
                status: (Status)po.Status,
                version: po.Version,
                updateAt: po.UpdateAt
            )).ToList();

            return templates;
        }
    }
}
