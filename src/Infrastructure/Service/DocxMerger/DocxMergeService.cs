using DocumentFormat.OpenXml.Packaging;
using Microsoft.Extensions.Options;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Interface;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Service.DocxMerger
{
    public class DocxMergeService : IDocxMergeService, ISingletonDependency
    {
        private readonly ILogger<DocxMergeService> _logger;
        private readonly DocxMergeOptions _defaultOptions;

        public DocxMergeService(
            ILogger<DocxMergeService> logger,
            IOptions<DocxMergeOptions>? defaultOptions = null)
        {
            _logger = logger;
            _defaultOptions = defaultOptions?.Value ?? new DocxMergeOptions();
        }

        public async Task MergeAsync(
            string baseDocxPath,
            IEnumerable<DocxMergeSection> sections,
            string outputPath,
            DocxMergeOptions? options = null,
            CancellationToken ct = default)
        {
            options ??= _defaultOptions;

            // ---- 参数校验 ----
            if (string.IsNullOrWhiteSpace(baseDocxPath))
                throw new ArgumentException("底稿路径不能为空", nameof(baseDocxPath));
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("输出路径不能为空", nameof(outputPath));
            if (!File.Exists(baseDocxPath))
                throw new FileNotFoundException("底稿文件不存在", baseDocxPath);

            var sectionList = (sections ?? Enumerable.Empty<DocxMergeSection>()).ToList();
            foreach (var s in sectionList)
            {
                if (string.IsNullOrWhiteSpace(s.FilePath))
                    throw new ArgumentException("子文档路径不能为空");
                if (!File.Exists(s.FilePath))
                    throw new FileNotFoundException("子文档不存在", s.FilePath);
            }

            ct.ThrowIfCancellationRequested();

            // ---- 准备输出目录 ----
            var outputDir = Path.GetDirectoryName(Path.GetFullPath(outputPath));
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            // ---- 复制底稿 ----
            File.Copy(baseDocxPath, outputPath, options.OverwriteOutput);

            _logger.LogInformation("以 {Base} 为底稿，准备追加 {Count} 个子文档到 {Out}",
                Path.GetFileName(baseDocxPath), sectionList.Count, outputPath);

            // ---- 打开输出，逐个追加 ----
            using (var targetDoc = WordprocessingDocument.Open(outputPath, true))
            {
                foreach (var section in sectionList)
                {
                    ct.ThrowIfCancellationRequested();

                    var displayName = section.DisplayName ?? Path.GetFileNameWithoutExtension(section.FilePath);
                    _logger.LogDebug("追加子文档 {File} ({Name})", section.FilePath, displayName);

                    SectionMerger.Append(
                        targetDoc,
                        section.FilePath,
                        options,
                        _logger);
                }

                targetDoc.MainDocumentPart!.Document.Save();
            }

            _logger.LogInformation("合并完成: {Out}", outputPath);

            // ---- 调试 XML ----
            if (options.DumpXmlForDiagnostics)
            {
                DocxXmlDumper.Dump(outputPath, options.DumpDirectory, _logger);
            }

            await Task.CompletedTask.ConfigureAwait(false);
        }

    }
}
