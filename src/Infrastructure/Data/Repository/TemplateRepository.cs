using Mapster;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share;
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
            if (aggregateRoot == null) throw new ArgumentNullException(nameof(aggregateRoot));

            var po = aggregateRoot.Adapt<src.Infrastructure.Data.Persistence.Template>();

            _context.Set<src.Infrastructure.Data.Persistence.Template>().Update(po);

            // 关联结构：先删后加（简单可靠）
            var existingStructure = await _context.Set<src.Infrastructure.Data.Persistence.TemplateStructure>()
                .FirstOrDefaultAsync(s => s.TemplateId == po.Id, ct);
            if (existingStructure != null)
                _context.Set<src.Infrastructure.Data.Persistence.TemplateStructure>().Remove(existingStructure);

            if (aggregateRoot.TemplateStructure != null)
            {
                var structurePo = new src.Infrastructure.Data.Persistence.TemplateStructure
                {
                    // 需要 Id、TemplateId、五个 count
                    Id = Guid.NewGuid(),
                    TemplateId = po.Id,
                    TestConditionCount = aggregateRoot.TemplateStructure.TestConditionCount,
                    TestMethodCount = aggregateRoot.TemplateStructure.TestMethodCount,
                    SampleDataAreaCount = aggregateRoot.TemplateStructure.SampleDataAreaCount,
                    SampleResultAreaCount = aggregateRoot.TemplateStructure.SampleResultAreaCount,
                    AfterWashDataCount = aggregateRoot.TemplateStructure.AfterWashDataCount
                };
                await _context.Set<src.Infrastructure.Data.Persistence.TemplateStructure>().AddAsync(structurePo, ct);
            }

            // 文本模板：先删后加
            var existingTexts = await _context.Set<src.Infrastructure.Data.Persistence.TestConditionTextTemplate>()
                .Where(t => t.TemplateId == po.Id)
                .ToListAsync(ct);
            foreach (var t in existingTexts)
                _context.Set<src.Infrastructure.Data.Persistence.TestConditionTextTemplate>().Remove(t);

            foreach (var text in aggregateRoot.TestConditionTextTemplates)
            {
                var textPo = new src.Infrastructure.Data.Persistence.TestConditionTextTemplate
                {
                    Id = Guid.NewGuid(),
                    TemplateId = po.Id,
                    TemplateIndex = text.TemplateIndex.ToJson(),
                    Text = text.Text
                };
                await _context.Set<src.Infrastructure.Data.Persistence.TestConditionTextTemplate>().AddAsync(textPo, ct);
            }
        }

        /// <summary>
        /// 查询聚合根
        /// </summary>
        /// <param name="aggregateRootId"></param>
        /// <param name="ct"></param>
        /// <returns>聚合根</returns>
        public async Task<Template?> GetByIdAsync(TemplateId aggregateRootId, CancellationToken ct)
        {
            var id = aggregateRootId.Value;

            var po = await _context.Set<src.Infrastructure.Data.Persistence.Template>()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id, ct);

            if (po == null) return null;

            var structurePo = await _context.Set<src.Infrastructure.Data.Persistence.TemplateStructure>()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.TemplateId == id, ct);

            var textPos = await _context.Set<src.Infrastructure.Data.Persistence.TestConditionTextTemplate>()
                .AsNoTracking()
                .Where(t => t.TemplateId == id)
                .ToListAsync(ct);

            return RebuildTemplate(po, structurePo, textPos);
        }

        /// <summary>
        /// 查询所有聚合根
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<List<Template>> GetAllAsync(CancellationToken ct)
        {
            var templatePOs = await _context.Set<src.Infrastructure.Data.Persistence.Template>()
                .AsNoTracking()
                .ToListAsync(ct);

            var structurePOs = await _context.Set<src.Infrastructure.Data.Persistence.TemplateStructure>()
                .AsNoTracking()
                .ToListAsync(ct);

            var structureDict = structurePOs
                .Where(ts => ts.TemplateId != null)
                .ToDictionary(ts => ts.TemplateId, ts => ts);

            var textPOs = await _context.Set<src.Infrastructure.Data.Persistence.TestConditionTextTemplate>()
                .AsNoTracking()
                .ToListAsync(ct);

            var textDict = textPOs
                .GroupBy(t => t.TemplateId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return templatePOs.Select(po =>
            {
                structureDict.TryGetValue(po.Id, out var s);
                textDict.TryGetValue(po.Id, out var texts);
                return RebuildTemplate(po, s, texts);
            }).ToList();
        }
        /// <summary>
        /// 根据索引键查询模板
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public IReadOnlyList<Template> FindByIndexKey(string key, object? value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return Array.Empty<Template>();

            var valueStr = value?.ToString() ?? "";

            // SQL Server: JSON_VALUE(template_index, '$.key')
            var pos = _context.Set<src.Infrastructure.Data.Persistence.Template>()
                .FromSqlRaw(
                "SELECT * FROM templates WHERE JSON_VALUE(template_index, '$.' + {0}) = {1}",
                    key, valueStr)
                .AsNoTracking()
                .ToList();

            if (pos.Count == 0) return Array.Empty<Template>();

            var ids = pos.Select(p => p.Id).ToList();

            var structurePos = _context.Set<src.Infrastructure.Data.Persistence.TemplateStructure>()
                .AsNoTracking()
                .Where(s => ids.Contains(s.TemplateId))
                .ToList();

            var structureDict = structurePos
                .Where(s => s.TemplateId != null)
                .ToDictionary(s => s.TemplateId, s => s);

            var textPos = _context.Set<src.Infrastructure.Data.Persistence.TestConditionTextTemplate>()
                .AsNoTracking()
                .Where(t => ids.Contains(t.TemplateId))
                .ToList();

            var textDict = textPos
                .GroupBy(t => t.TemplateId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return pos.Select(po =>
            {
                structureDict.TryGetValue(po.Id, out var s);
                textDict.TryGetValue(po.Id, out var texts);
                return RebuildTemplate(po, s, texts);
            }).ToList();
        }

        /// <summary>
        /// 根据模板URL查询模板
        /// </summary>
        /// <param name="templateUrl"></param>
        /// <returns></returns>
        public Template? FindByUrl(string templateUrl)
        {
            if (string.IsNullOrWhiteSpace(templateUrl))
                return null;

            var po = _context.Set<src.Infrastructure.Data.Persistence.Template>()
                .AsNoTracking()
                .FirstOrDefault(t => t.TemplateUrl == templateUrl);

            if (po == null) return null;

            var structurePo = _context.Set<src.Infrastructure.Data.Persistence.TemplateStructure>()
                .AsNoTracking()
                .FirstOrDefault(s => s.TemplateId == po.Id);

            var textPos = _context.Set<src.Infrastructure.Data.Persistence.TestConditionTextTemplate>()
                .AsNoTracking()
                .Where(t => t.TemplateId == po.Id)
                .ToList();

            return RebuildTemplate(po, structurePo, textPos);
        }

        /// <summary>
        /// 从 PO 重建领域聚合根（含结构 + 文本模板）
        /// </summary>
        private static Template RebuildTemplate(
            src.Infrastructure.Data.Persistence.Template po,
            src.Infrastructure.Data.Persistence.TemplateStructure? structurePo,
            List<src.Infrastructure.Data.Persistence.TestConditionTextTemplate>? textTemplatePos)
        {
            src.Domain.Aggregeates.TemplateContext.TemplateStructure? structure = null;
            if (structurePo != null)
            {
                structure = src.Domain.Aggregeates.TemplateContext.TemplateStructure.Create(
                    structurePo.TestConditionCount,
                    structurePo.TestMethodCount,
                    structurePo.SampleDataAreaCount,
                    structurePo.SampleResultAreaCount,
                    structurePo.AfterWashDataCount,
                    new TemplateId(po.Id));
            }

            var texts = textTemplatePos != null && textTemplatePos.Count > 0
                ? textTemplatePos.Select(t =>
                    src.Domain.Aggregeates.TemplateContext.TestConditionTextTemplate.Create(
                        TemplateIndex.FromJson(t.TemplateIndex),
                        t.Text)).ToList()
                : new List<src.Domain.Aggregeates.TemplateContext.TestConditionTextTemplate>();

            return Template.Rebuild(
                id: new TemplateId(po.Id),
                templateName: po.TemplateName,
                templateUrl: po.TemplateUrl,
                site: (Site)po.Site,
                status: (Status)po.Status,
                fileType: (TemplateFileType)po.FileType,
                businessCategory: po.BusinessCategory,
                templateIndex: TemplateIndex.FromJson(po.TemplateIndex),
                templateStructure: structure,
                version: po.Version,
                updateAt: po.UpdateAt,
                testConditionTextTemplates: texts);
        }
    }
}
