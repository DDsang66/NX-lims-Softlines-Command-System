using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

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
            Site site)
        {
            // 1. 参数校验
            if (string.IsNullOrWhiteSpace(templateName))
                throw new ArgumentException("模板名称不能为空", nameof(templateName));

            // 2. 创建实体并赋予初始状态
            var template = new Template
            {
                Id = id,
                TemplateName = templateName,
                Site = site,
                Status = Status.Draft, // 新创建的模板默认为草稿状态
                Version = 1,
                UpdateAt = DateTime.Now
            };

            template.TemplateUrl = template.GenerateTemplateUrl();

            // 3. 发布领域事件 (假设你的 AggregateRoot 提供了 AddDomainEvent 方法)
            // template.AddDomainEvent(new TemplateCreatedEvent(id, templateName));

            return template;
        }

        /// <summary>
        /// 重建模板（从基础设施层/数据库还原领域对象时使用）
        /// </summary>
        public static Template Rebuild(
            TemplateId id,
            string templateName,
            string templateUrl,
            Site site,
            Status status,
            int version,
            DateTime updateAt)
        {
            return new Template
            {
                Id = id,
                TemplateName = templateName,
                TemplateUrl = templateUrl,
                Site = site,
                Status = status,
                Version = version,
                UpdateAt = updateAt
            };
        }

        /// <summary>
        /// 更新模板基本信息
        /// </summary>
        public void Update(string templateName, string templateUrl, Site site)
        {
            // 1. 业务规则校验：只有草稿状态才允许修改基本信息
            if (Status != Status.Draft)
                throw new InvalidOperationException("只有草稿状态的模板才允许修改基本信息");

            // 2. 参数校验
            if (string.IsNullOrWhiteSpace(templateName))
                throw new ArgumentException("模板名称不能为空", nameof(templateName));
            if (string.IsNullOrWhiteSpace(templateUrl))
                throw new ArgumentException("模板URL不能为空", nameof(templateUrl));

            // 3. 更新状态
            TemplateName = templateName;
            TemplateUrl = templateUrl;
            Site = site;
            UpdateAt = DateTime.Now;

            // 4. 发布领域事件
            // AddDomainEvent(new TemplateUpdatedEvent(Id));
        }

        /// <summary>
        /// 获取模板 URL
        /// </summary>
        /// <returns></returns>
        public string GetTemplateUrl() => TemplateUrl;

        /// <summary>
        /// 提交模板（将状态从 Draft 变更为 Published，并版本号+1）
        /// </summary>
        public void Publish()
        {
            if (Status != Status.Draft)
                throw new InvalidOperationException("只有草稿状态的模板才能发布");

            Status = Status.Active; 
            Version += 1;
            UpdateAt = DateTime.Now;

            // AddDomainEvent(new TemplatePublishedEvent(Id, Version));
        }

        /// <summary>
        /// 回滚至上一个版本或指定版本
        /// </summary>
        /// <param name="targetVersion">目标版本号。如果为null，则回滚至上一个版本</param>
        public void Rollback(int? targetVersion = null)
        {
            // 1. 确定目标版本
            int expectedVersion = targetVersion ?? Version - 1;

            // 2. 业务规则校验
            if (expectedVersion <= 0)
                throw new InvalidOperationException("版本号不能小于或等于0，无法回滚");

            if (expectedVersion >= Version)
                throw new InvalidOperationException($"目标版本 {expectedVersion} 大于或等于当前版本 {Version}，无法回滚");

            // 3. 执行回滚逻辑
            Status = Status.Draft; // 回滚后通常需要重新修改，所以状态切回 Draft
            UpdateAt = DateTime.Now;

            // 注意：回滚操作通常意味着我们需要从历史记录中恢复 TemplateUrl 等数据。
            // 在 CQRS 架构中，这部分数据恢复逻辑通常在应用层处理（通过事件溯源或查询历史表），
            // 聚合根这里只负责维护版本和状态的正确流转。

            // 4. 发布领域事件
            // AddDomainEvent(new TemplateRolledBackEvent(Id, Version));
        }


        /// <summary>
        /// 根据业务规则自动生成模板 URL
        /// </summary>
        /// <returns>生成的 URL 字符串</returns>
        private string GenerateTemplateUrl()
        {
            // 这里的生成规则需要根据你的实际业务需求定制
            // 示例规则: /templates/{Site}/{TemplateName}_{当前时间戳}.html

            string siteName = Site.ToString(); // 假设 Site 枚举重写了 ToString 或直接使用名称
            string safeName = TemplateName.Replace(" ", "_"); // 简单处理空格
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

            return $"/templates/{siteName}/{safeName}_{timestamp}.html";
        }
    }
}

