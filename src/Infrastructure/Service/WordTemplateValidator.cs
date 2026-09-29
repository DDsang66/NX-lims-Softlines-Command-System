using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TemplateContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Service
{
    public class WordTemplateValidator : ITemplateValidator, IScopedDependency
    {
        private readonly IWebHostEnvironment _env;

        public WordTemplateValidator(IWebHostEnvironment env)
        {
            _env = env;
        }

        /// <summary>
        /// 验证模板文件
        /// </summary>
        /// <param name="template"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<ValidationResult> ValidateAsync(Template template, CancellationToken ct)
        {
            if (template.TemplateStructure == null)
                return new ValidationResult(false, "模板结构未配置");

            // 1. 找到磁盘上的 .docx
            var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
            var relative = template.TemplateUrl.TrimStart('/', '\\');
            var filePath = Path.Combine(webRoot, relative);

            if (!File.Exists(filePath))
                return new ValidationResult(false, $"模板文件不存在: {filePath}");

            // 2. 打开 .docx，检查书签
            var bookmarkCheck = ValidateBookmarks(filePath, template.TemplateStructure);
            if (!bookmarkCheck.IsValid)
                return bookmarkCheck;

            // 3. 安全性检查
            var securityCheck = ValidateSecurity(filePath);
            if (!securityCheck.IsValid)
                return securityCheck;

            return new ValidationResult(true, "验证通过");
        }

        /// <summary>
        /// 书签完整性检查
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="structure"></param>
        /// <returns></returns>
        private ValidationResult ValidateBookmarks(string filePath, TemplateStructure structure)
        {
            // 用 OpenXML SDK 打开
            using var doc = WordprocessingDocument.Open(filePath, false);
            var body = doc.MainDocumentPart?.Document?.Body;
            if (body == null)
                return new ValidationResult(false, "文档内容为空");

            // 收集所有书签
            var bookmarks = body.Descendants<BookmarkStart>()
                .Select(b => b.Name?.Value)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 按 TemplateStructure 检查书签数量
            var missing = new List<string>();

            //// 比如：需要 TestConditionCount 个 "TestCondition_X" 书签
            //for (int i = 1; i <= structure.TestConditionCount; i++)
            //{
            //    var name = $"TestCondition_{i}";
            //    if (!bookmarks.Contains(name)) missing.Add(name);
            //}
            //// 同理检查 TestMethodCount / SampleDataAreaCount / ...

            //for (int i = 1; i <= structure.TestMethodCount; i++) 
            //{
            //    var name = $"TestMethod_{i}";
            //    if (!bookmarks.Contains(name)) missing.Add(name);
            //}

            if (structure.TestConditionCount > 0) 
            {
                if (!bookmarks.Contains("TestCondition")) missing.Add("TestCondition");
            }

            if (structure.TestMethodCount > 0) 
            {
                if (!bookmarks.Contains("TestMethod")) missing.Add("TestMethod"); 
            }

            for (int i = 1; i <= structure.SampleDataAreaCount; i++) 
            {
                var name = $"Sample_data_{i}";
                if (!bookmarks.Contains(name)) missing.Add(name);
            }

            for (int i = 1; i <= structure.AfterWashDataCount; i++) 
            {
                var name = $"After_wash_data_{i}";
                if (!bookmarks.Contains(name)) missing.Add(name);
            }

            for (int i = 1; i <= structure.SampleResultAreaCount; i++) 
            {
                var name = $"Sample_result_{i}";
                if (!bookmarks.Contains(name)) missing.Add(name);
            }

            if (missing.Count > 0)
                return new ValidationResult(false, $"缺少书签: {string.Join(", ", missing)}");

            return new ValidationResult(true, "书签验证通过");
        }

        /// <summary>
        /// 安全性检查
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private ValidationResult ValidateSecurity(string filePath)
        {
            // 用 OpenXML SDK 检查宏、外部链接、嵌入对象等
            using var doc = WordprocessingDocument.Open(filePath, false);

            // 1. 检查宏（.docm 才有，.docx 一般没有）
            var hasMacros = doc.MainDocumentPart?.Document
                .Descendants<DocumentFormat.OpenXml.Wordprocessing.AltChunk>().Any() ?? false;
            // 更准的是检查 VBA 项目，但 .docx 不允许宏

            // 2. 检查外部链接
            var embeddedObjects = doc.MainDocumentPart?.Parts
                .Where(p => p.OpenXmlPart is EmbeddedObjectPart)
                .ToList() ?? new List<IdPartPair>();

            if (embeddedObjects.Any())
                return new ValidationResult(false, "文档包含嵌入对象，不允许发布");

            // 3. 检查嵌入对象（OLE）
            var externalRels = doc.MainDocumentPart?.Parts
                .SelectMany(p => p.OpenXmlPart.ExternalRelationships)
                .ToList() ?? new List<ExternalRelationship>();

            if (externalRels.Any())
                return new ValidationResult(false, "文档包含外部链接，不允许发布");

            // 4. 检查 AltChunk（嵌入 HTML/RTF 等）
            var altChunks = doc.MainDocumentPart?.Document?.Body?
                .Descendants<DocumentFormat.OpenXml.Wordprocessing.AltChunk>().ToList()
                ?? new List<DocumentFormat.OpenXml.Wordprocessing.AltChunk>();
            if (altChunks.Any())
                return new ValidationResult(false, "文档包含 AltChunk，不允许发布");

            return new ValidationResult(true, "安全性验证通过");
        }
    }
}
