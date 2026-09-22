using System.Text;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;

namespace NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext
{
    public sealed class Template : AggregateRoot<TemplateId, string>
    {
        /// <summary>
        /// 模板名称
        /// </summary>
        public string TemplateName { get; private set; } = string.Empty;

        /// <summary>
        /// 模板的url
        /// </summary>
        public string TemplateUrl { get; private set; } = string.Empty;

        /// <summary>
        /// 地区站点
        /// </summary>
        public Site Site { get; private set; } = Site.NB; // 假设NB是默认值，请根据实际枚举调整

        /// <summary>
        /// 当前datasheet状态
        /// </summary>
        public Status Status { get; private set; } = Status.Draft;

        // Template 聚合根内部新增属性
        /// <summary>
        /// 模板文件类型
        /// </summary>
        public TemplateFileType FileType { get; private set; } = TemplateFileType.Docx;

        /// <summary>
        /// 模板结构
        /// </summary>
        public TemplateStructure TemplateStructure { get; private set; }

        /// <summary>
        /// 模板索引
        /// </summary>
        public TemplateIndex TemplateIndex { get; private set; }

        /// <summary>
        /// 该模板关联的测试条件文本模板集合（多对多）
        /// </summary>
        private readonly List<TestConditionTextTemplate> _testConditionTextTemplates = new();

        public IReadOnlyCollection<TestConditionTextTemplate> TestConditionTextTemplates
            => _testConditionTextTemplates.AsReadOnly();
        /// <summary>
        /// 业务子分类文件夹名称 (如 Common_FLAM, Common_PHY, 买家特定名称等)
        /// </summary>
        public string BusinessCategory { get; private set; } = string.Empty;

        /// <summary>
        /// 当前模板版本
        /// </summary>
        public int Version { get; private set; } = 1;

        /// <summary>
        /// 变更时间
        /// </summary>
        public DateTime UpdateAt { get; private set; } = DateTime.Now;

        // EF Core 或 ORM 需要的无参构造函数（建议设置为 internal 或 protected，限制外部直接new）
        private Template() { }

        /// <summary>
        /// 创建新模板
        /// </summary>
        public static Template Create(
            TemplateId id,
            string templateName,
            Site site,
            TemplateFileType fileType,
            string businessCategory,
            TemplateIndex? templateIndex,
            TemplateStructure? templateStructure,
            IEnumerable<TestConditionTextTemplate>? testConditionTextTemplates = null,
            DateTime? now = null)
        {
            // 1. 参数校验
            if (id == null)
                throw new ArgumentNullException(nameof(id), "模板ID不能为空");

            if (string.IsNullOrWhiteSpace(templateName))
                throw new ArgumentException("模板名称不能为空", nameof(templateName));

            if (string.IsNullOrWhiteSpace(businessCategory))
                throw new ArgumentException("业务分类不能为空", nameof(businessCategory));

            // 时间戳从外部传入, 便于测试断言生成的文件名; 默认取当前时间
            var createdAt = now ?? DateTime.Now;

            // 创建实体
            var template = new Template
            {
                Id = id,
                TemplateName = templateName.Trim(),
                Site = site,
                Status = Status.Draft,
                Version = 1,
                FileType = fileType,
                // 分类会进 URL 路径, 与 TemplateName 走同一套净化, 挡住路径穿越
                BusinessCategory = SanitizeSegment(businessCategory, nameof(businessCategory)),
                TemplateIndex = templateIndex ?? TemplateIndex.Empty(),
                TemplateStructure = templateStructure,
                UpdateAt = createdAt
            };

            // 可选：关联测试条件文本模板
            if (testConditionTextTemplates != null)
            {
                foreach (var textTemplate in testConditionTextTemplates)
                {
                    template.AddTextTemplate(textTemplate);
                }
            }

            template.TemplateUrl = template.GenerateTemplateUrl(createdAt);

            // template.AddDomainEvent(new TemplateCreatedEvent(id, templateName));

            return template;
        }


        public static Template Rebuild(
            TemplateId id,
            string templateName,
            string templateUrl,
            Site site,
            Status status,
            TemplateFileType fileType,
            string businessCategory,
            TemplateIndex templateIndex,
            TemplateStructure templateStructure,
            int version,
            DateTime updateAt,
            IEnumerable<TestConditionTextTemplate>? testConditionTextTemplates = null)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));

            var template = new Template
            {
                Id = id,
                TemplateName = templateName,
                TemplateUrl = templateUrl,
                Site = site,
                Status = status,
                FileType = fileType,
                BusinessCategory = businessCategory,
                TemplateIndex = templateIndex ?? TemplateIndex.Empty(),
                TemplateStructure = templateStructure,
                Version = version,
                UpdateAt = updateAt
            };

            if (testConditionTextTemplates != null)
            {
                template._testConditionTextTemplates.AddRange(testConditionTextTemplates);
            }

            return template;
        }

        /// <summary>
        /// 更新模板基本信息（仅草稿状态允许）
        /// </summary>
        public void Update(string templateName, Site site, string businessCategory)
        {
            EnsureDraftStatus("修改基本信息");

            if (string.IsNullOrWhiteSpace(templateName))
                throw new ArgumentException("模板名称不能为空", nameof(templateName));

            if (string.IsNullOrWhiteSpace(businessCategory))
                throw new ArgumentException("业务分类不能为空", nameof(businessCategory));

            // 只换目录、保留文件名: 若连文件名一起重生成, 盘上已存在的文件就与 URL 失联了。
            // 文件名只在版本变更(Publish / Rollback)时重生成, 那时调用方会同步搬文件。
            string fileName = FileNameOf(TemplateUrl);

            TemplateName = templateName.Trim();
            Site = site;
            BusinessCategory = SanitizeSegment(businessCategory, nameof(businessCategory));
            TemplateUrl = BuildPath(fileName);
            UpdateAt = DateTime.Now;

            // AddDomainEvent(new TemplateUpdatedEvent(Id));
        }

        /// <summary>
        /// 更新模板结构配置
        /// </summary>
        public void UpdateStructure(TemplateStructure structure)
        {
            if (structure == null)
                throw new ArgumentNullException(nameof(structure));

            EnsureDraftStatus("修改模板结构");

            TemplateStructure = structure;
            UpdateAt = DateTime.Now;
        }

        /// <summary>
        /// 添加一个测试条件文本模板关联
        /// </summary>
        public void AddTextTemplate(TestConditionTextTemplate textTemplate)
        {
            if (textTemplate == null)
                throw new ArgumentNullException(nameof(textTemplate));

            // 按 TemplateIndex 去重，避免同一索引重复关联
            if (_testConditionTextTemplates.Any(t => t.TemplateIndex.Equals(textTemplate.TemplateIndex)))
                return;

            _testConditionTextTemplates.Add(textTemplate);
            UpdateAt = DateTime.Now;
        }

        /// <summary>
        /// 移除一个文本模板关联（只解除关系，不删除实体）
        /// </summary>
        public void RemoveTextTemplate(TestConditionTextTemplate textTemplate)
        {
            if (textTemplate == null)
                throw new ArgumentNullException(nameof(textTemplate));

            if (_testConditionTextTemplates.Remove(textTemplate))
                UpdateAt = DateTime.Now;
        }

        /// <summary>
        /// 根据索引条件查找本模板下的文本模板
        /// </summary>
        public TestConditionTextTemplate? FindTextTemplate(IReadOnlyDictionary<string, object?> conditions)
        {
            if (conditions == null || conditions.Count == 0)
                return null;

            var matched = _testConditionTextTemplates
                .Where(t => t.Matches(conditions))
                .ToList();

            if (matched.Count > 1)
                throw new InvalidOperationException(
                    $"文本模板索引命中 {matched.Count} 个，无法唯一确定");

            return matched.FirstOrDefault();
        }

        /// <summary>
        /// 更新模板索引
        /// </summary>
        public void UpdateIndex(TemplateIndex index)
        {
            if (index == null)
                throw new ArgumentNullException(nameof(index));

            EnsureDraftStatus("修改模板索引");

            TemplateIndex = index;
            UpdateAt = DateTime.Now;
        }
        /// <summary>
        /// 获取模板 URL
        /// </summary>
        /// <returns></returns>
        public string GetTemplateUrl() => TemplateUrl;

        /// <summary>
        /// 重新生成 TemplateUrl（重传模板文件、或文件名规则变化时调用）。
        /// 会产生新的时间戳 —— 调用方必须同步把盘上文件搬到新路径，否则 URL 指向空文件。
        /// </summary>
        public void RegenerateUrl(DateTime? now = null)
        {
            EnsureDraftStatus("重新生成URL");

            TemplateUrl = GenerateTemplateUrl(now ?? DateTime.Now);
            UpdateAt = DateTime.Now;
        }

        /// <summary>
        /// 发布模板（版本号 +1，并按新版本重新生成文件名）。
        /// URL 会变 —— 调用方必须把盘上文件另存为新版本路径后，才能提交本次变更。
        /// </summary>
        public void Publish(DateTime? now = null)
        {
            if (Status != Status.Draft)
                throw new InvalidOperationException("只有草稿状态的模板才能发布");

            if (TemplateStructure == null)
                throw new InvalidOperationException("模板结构未配置，无法发布");

            if (TemplateIndex == null || TemplateIndex.Values.Count == 0)
                throw new InvalidOperationException("模板索引未配置，无法发布");

            var publishedAt = now ?? DateTime.Now;

            Status = Status.Active;
            Version += 1;
            // 这里不能走 RegenerateUrl: 它带 EnsureDraftStatus, 而状态刚被置为 Active
            TemplateUrl = GenerateTemplateUrl(publishedAt);
            UpdateAt = publishedAt;

            // AddDomainEvent(new TemplatePublishedEvent(Id, Version));
        }

        /// <summary>
        /// 回滚至指定版本（版本号真正回退，并按目标版本重新生成文件名）。
        /// URL 会变 —— 调用方必须把盘上文件搬回对应版本的路径后，才能提交本次变更。
        /// </summary>
        public void Rollback(int? targetVersion = null, DateTime? now = null)
        {
            int expectedVersion = targetVersion ?? Version - 1;

            if (expectedVersion <= 0)
                throw new InvalidOperationException("版本号不能小于或等于0，无法回滚");

            if (expectedVersion >= Version)
                throw new InvalidOperationException($"目标版本 {expectedVersion} 大于或等于当前版本 {Version}，无法回滚");

            var rolledBackAt = now ?? DateTime.Now;

            Status = Status.Draft;
            // 版本号必须真的退回去, 否则 URL 里的 _v{n}_ 会与实体版本长期不一致
            Version = expectedVersion;
            TemplateUrl = GenerateTemplateUrl(rolledBackAt);
            UpdateAt = rolledBackAt;

            // 注意：具体数据恢复由应用层通过事件溯源或历史表完成
            // AddDomainEvent(new TemplateRolledBackEvent(Id, expectedVersion));
        }

        /// <summary>
        /// 软删除 / 归档（可选）
        /// </summary>
        public void Archive()
        {
            if (Status == Status.Archived)
                throw new InvalidOperationException("模板已归档");

            Status = Status.Archived;
            UpdateAt = DateTime.Now;

            // AddDomainEvent(new TemplateArchivedEvent(Id));
        }

        private void EnsureDraftStatus(string operation)
        {
            if (Status != Status.Draft)
                throw new InvalidOperationException($"只有草稿状态的模板才允许{operation}");
        }

        /// <summary>
        /// 生成模板相对 WebRoot 的访问路径（不含 host，也不含 wwwroot 段）。
        /// 例: /DocxModel/Common_PHY/PHY_Weight_v1_20260922143000.docx
        ///
        /// 存相对路径而非绝对 URL: wwwroot 是静态文件的物理根, 不是 URL 段,
        /// 拼进 URL 会 404; 而绝对 URL 会把请求时的 host 烤进库里, 换端口或走反代即失效。
        /// </summary>
        private string GenerateTemplateUrl(DateTime now)
            => BuildPath(BuildFileName(now));

        /// <summary>目录段 —— 只随 FileType / BusinessCategory 变化</summary>
        private string BuildPath(string fileName)
            => $"/{ModelFolder()}/{BusinessCategory}/{fileName}";

        /// <summary>文件名段 —— 只随 TemplateName / Version 变化</summary>
        private string BuildFileName(DateTime now)
            => $"{SanitizeSegment(TemplateName, nameof(TemplateName))}_v{Version}_{now:yyyyMMddHHmmss}{Extension()}";

        /// <summary>从现有 URL 取回文件名（URL 约定总是以文件名结尾）</summary>
        private static string FileNameOf(string url)
            => string.IsNullOrWhiteSpace(url) ? string.Empty : url[(url.LastIndexOf('/') + 1)..];

        private string ModelFolder() => FileType == TemplateFileType.Docx ? "DocxModel" : "ExcelModel";

        private string Extension() => FileType == TemplateFileType.Docx ? ".docx" : ".xlsx";

        /// <summary>
        /// 净化一个路径段（模板名 / 业务分类）。用户输入会直接进 URL 路径，
        /// 路径分隔符与 ".." 一律拒绝而非静默改写 —— 这类输入是攻击或错误，不该被"修好"。
        /// 其余非法文件名字符与空格统一替换为下划线。
        /// </summary>
        private static string SanitizeSegment(string raw, string paramName)
        {
            if (string.IsNullOrWhiteSpace(raw))
                throw new ArgumentException("路径段不能为空", paramName);

            string trimmed = raw.Trim();

            if (trimmed.Contains('/') || trimmed.Contains('\\') || trimmed.Contains(".."))
                throw new ArgumentException($"路径段不能包含路径分隔符或 ..: {raw}", paramName);

            var invalid = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(trimmed.Length);
            foreach (char c in trimmed)
            {
                sb.Append(c == ' ' || Array.IndexOf(invalid, c) >= 0 ? '_' : c);
            }

            // 首尾的点会拼出隐藏文件或 "..", 统一削掉
            string sanitized = sb.ToString().Trim('.', '_');
            if (sanitized.Length == 0)
                throw new ArgumentException($"路径段净化后为空: {raw}", paramName);

            return sanitized;
        }
    }
}

