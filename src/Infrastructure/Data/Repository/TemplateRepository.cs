using DocumentFormat.OpenXml.Office2010.Excel;
using Mapster;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext.Enums;
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
                throw new ArgumentNullException(nameof(aggregateRoot));

            var po = new Persistence.Template
            {
                Id = aggregateRoot.Id.Value,
                TemplateName = aggregateRoot.TemplateName,
                TemplateUrl = aggregateRoot.TemplateUrl,
                Site = (int)aggregateRoot.Site,
                Status = (int)aggregateRoot.Status,
                FileType = (int)aggregateRoot.FileType,
                BusinessCategory = aggregateRoot.BusinessCategory,
                TemplateIndex = aggregateRoot.TemplateIndex?.ToJson() ?? "{}",   // ★ 手动 ToJson
                Version = aggregateRoot.Version,
                UpdateAt = aggregateRoot.UpdateAt
            };

            await _context.Set<Persistence.Template>().AddAsync(po, ct);

            // 结构
            if (aggregateRoot.TemplateStructure != null)
            {
                var structurePo = new Persistence.TemplateStructure
                {
                    Id = Guid.NewGuid(),
                    TemplateId = po.Id,
                    TestConditionCount = aggregateRoot.TemplateStructure.TestConditionCount,
                    TestMethodCount = aggregateRoot.TemplateStructure.TestMethodCount,
                    SampleDataAreaCount = aggregateRoot.TemplateStructure.SampleDataAreaCount,
                    SampleResultAreaCount = aggregateRoot.TemplateStructure.SampleResultAreaCount,
                    AfterWashDataCount = aggregateRoot.TemplateStructure.AfterWashDataCount
                };
                await _context.Set<Persistence.TemplateStructure>().AddAsync(structurePo, ct);
            }

            // 文本模板
            foreach (var text in aggregateRoot.TestConditionTextTemplates)
            {
                var textPo = new Persistence.TestConditionTextTemplate
                {
                    Id = Guid.NewGuid(),
                    TemplateId = po.Id,
                    TemplateIndex = text.TemplateIndex.ToJson(),   // ★ 手动 ToJson
                    Text = text.Text
                };
                await _context.Set<Persistence.TestConditionTextTemplate>().AddAsync(textPo, ct);
            }
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

            // ==================== 2. TemplateStructure：一对一，直接更新 ====================
            var existingStructure = await _context.Set<Persistence.TemplateStructure>()
                .FirstOrDefaultAsync(s => s.TemplateId == po.Id, ct);

            if (aggregateRoot.TemplateStructure != null)
            {
                if (existingStructure != null)
                {
                    // 更新已有
                    existingStructure.TestConditionCount = aggregateRoot.TemplateStructure.TestConditionCount;
                    existingStructure.TestMethodCount = aggregateRoot.TemplateStructure.TestMethodCount;
                    existingStructure.SampleDataAreaCount = aggregateRoot.TemplateStructure.SampleDataAreaCount;
                    existingStructure.SampleResultAreaCount = aggregateRoot.TemplateStructure.SampleResultAreaCount;
                    existingStructure.AfterWashDataCount = aggregateRoot.TemplateStructure.AfterWashDataCount;
                }
                else
                {
                    // 新建
                    var structurePo = new Persistence.TemplateStructure
                    {
                        Id = Guid.NewGuid(),
                        TemplateId = po.Id,
                        TestConditionCount = aggregateRoot.TemplateStructure.TestConditionCount,
                        TestMethodCount = aggregateRoot.TemplateStructure.TestMethodCount,
                        SampleDataAreaCount = aggregateRoot.TemplateStructure.SampleDataAreaCount,
                        SampleResultAreaCount = aggregateRoot.TemplateStructure.SampleResultAreaCount,
                        AfterWashDataCount = aggregateRoot.TemplateStructure.AfterWashDataCount
                    };
                    await _context.Set<Persistence.TemplateStructure>().AddAsync(structurePo, ct);
                }
            }
            else if (existingStructure != null)
            {
                // 新的没有 structure，删掉旧的
                _context.Set<Persistence.TemplateStructure>().Remove(existingStructure);
            }

            // ==================== 3. TestConditionTextTemplates：一对多，按 TemplateIndex 对比 ====================
            await SyncTextTemplatesAsync(po.Id, aggregateRoot.TestConditionTextTemplates, ct);
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
        /// 同步文本模板：按 TemplateIndex 的 JSON 做键，做增/删/改
        /// </summary>
        private async Task SyncTextTemplatesAsync(
            string templateId,
            IEnumerable<Domain.Aggregeates.TemplateContext.TestConditionTextTemplate> newTexts,
            CancellationToken ct)
        {
            // 1. 一次性查出所有旧记录
            var existingList = await _context.Set<Persistence.TestConditionTextTemplate>()
                .Where(t => t.TemplateId == templateId)
                .ToListAsync(ct);

            // 2. 用 TemplateIndex JSON 做键，构建字典
            //    注意：如果同一个 TemplateIndex 有多条（数据异常），用 GroupBy 取第一条
            var existingDict = existingList
                .GroupBy(t => t.TemplateIndex ?? "")
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

            // 3. 新集合转 List，一次遍历
            var newList = newTexts?.ToList() ?? new List<Domain.Aggregeates.TemplateContext.TestConditionTextTemplate>();

            // 4. 新键集合，用于快速判断「旧的要删哪些」
            var newKeys = new HashSet<string>(
                newList.Select(t => t.TemplateIndex.ToJson()),
                StringComparer.Ordinal);

            // 5. 删除：旧的键不在新的里
            foreach (var existing in existingList)
            {
                var key = existing.TemplateIndex ?? "";
                if (!newKeys.Contains(key))
                {
                    _context.Set<Persistence.TestConditionTextTemplate>().Remove(existing);
                }
            }

            // 6. 新增/更新：遍历新的
            foreach (var newText in newList)
            {
                var key = newText.TemplateIndex.ToJson();

                if (existingDict.TryGetValue(key, out var existing))
                {
                    // 存在则更新 Text（Index 相同，只有 Text 可能变）
                    if (!string.Equals(existing.Text, newText.Text, StringComparison.Ordinal))
                    {
                        existing.Text = newText.Text;
                    }
                    // 从字典移除，剩下的就是「已处理」的
                    existingDict.Remove(key);
                }
                else
                {
                    // 新增
                    var textPo = new Persistence.TestConditionTextTemplate
                    {
                        Id = Guid.NewGuid(),
                        TemplateId = templateId,
                        TemplateIndex = key,
                        Text = newText.Text
                    };
                    await _context.Set<Persistence.TestConditionTextTemplate>().AddAsync(textPo, ct);
                }
            }

            // 7. 此时 existingDict 里剩下的，就是「旧的里没被新的匹配到的」——
            //    但第 5 步已经按 newKeys 删过了，这里应该是空的。
            //    如果 TemplateIndex 有重复导致分组只取第一条，剩下的要兜底删掉。
            foreach (var leftover in existingDict.Values)
            {
                _context.Set<Persistence.TestConditionTextTemplate>().Remove(leftover);
            }
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
                    src.Domain.Aggregeates.TemplateContext.TestConditionTextTemplate.Rebuild(
                        t.Id,
                        TemplateIndex.FromJson(t.TemplateIndex),
                        t.Text)).ToList()
                : new List<src.Domain.Aggregeates.TemplateContext.TestConditionTextTemplate>();

            return Template.Rebuild(
                id: new TemplateId(po.Id),
                templateName: po.TemplateName,
                templateUrl: po.TemplateUrl,
                site: (Site)po.Site,
                status: (TemplateStatus)po.Status,
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
