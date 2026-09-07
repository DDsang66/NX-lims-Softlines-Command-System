// ============================================================
// 干燥速率报告文件存储 (ReportFileStore)
//
// 职责: 干燥速率测试的 DOCX 报告以"文件"形式持久化（决策14: 不落结构化库）。
//       所有报告存独立子目录 wwwroot/DocxModel/SaveDocx/DryingRate/，
//       与现有物理克重报告目录(SaveDocx/)互不干扰，随时可清不影响现有数据。
//
// 命名规范（照 PhysicalWeight）:
//   文件名 = {报告号}_{yyMMddHHmmss}_{mode}.docx
//   例: 11111_260826143512_nf5022.docx → 报告号11111, 2026-08-26 14:35:12, nf5022
//   - 时间戳精确到秒 → 同一报告号多次生成不撞名
//   - mode 固定小写末段 → 列表按模式过滤、下载按文件名定位
//
// 安全:
//   - 写侧( SaveReport / PrepareReportFile )与读侧( ResolvePath )都做
//     Path.GetFileName 往返校验, 拒绝 .. 路径穿越与非法字符
//   - ListReports 只扫描本目录 *.docx，不碰其他目录
// ============================================================
using System.Globalization;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.ReportStorage;

/// <summary>
/// 干燥速率 DOCX 报告文件存储实现。
/// 注入 IWebHostEnvironment 定位 wwwroot，报告子目录 SaveDocx/DryingRate/ 在首次保存时自动创建。
/// </summary>
public class ReportFileStore : IReportFileStore, IScopedDependency
{
    /// <summary>报告子目录（相对 wwwroot）</summary>
    private static readonly string ReportDirRelative = Path.Combine("DocxModel", "SaveDocx", "DryingRate");

    private readonly IWebHostEnvironment _env;

    public ReportFileStore(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>报告目录完整路径（wwwroot/DocxModel/SaveDocx/DryingRate）</summary>
    private string ReportDir => Path.Combine(_env.WebRootPath, ReportDirRelative);

    /// <inheritdoc />
    public string SaveReport(byte[] content, string reportNumber, string mode)
    {
        if (content == null || content.Length == 0)
            throw new ArgumentException("报告内容不能为空", nameof(content));

        string fileName = BuildFileName(reportNumber, mode);
        Directory.CreateDirectory(ReportDir);
        System.IO.File.WriteAllBytes(Path.Combine(ReportDir, fileName), content);
        return fileName;
    }

    /// <inheritdoc />
    public string PrepareReportFile(string templateRelativePath, string reportNumber, string mode)
    {
        if (string.IsNullOrWhiteSpace(templateRelativePath))
            throw new ArgumentException("模板路径不能为空", nameof(templateRelativePath));

        string templatePath = Path.Combine(_env.WebRootPath, templateRelativePath);
        if (!System.IO.File.Exists(templatePath))
            throw new InvalidOperationException($"报告模板不存在: {templateRelativePath}");

        string fileName = BuildFileName(reportNumber, mode);
        Directory.CreateDirectory(ReportDir);
        string targetPath = Path.Combine(ReportDir, fileName);
        System.IO.File.Copy(templatePath, targetPath, overwrite: true);
        return targetPath;
    }

    /// <summary>
    /// 净化报告号并生成文件名 {报告号}_{yyMMddHHmmss}_{mode}.docx。
    /// 报告号写侧校验（与读侧 ResolvePath 同强度）: 只允许纯文件名, 拒绝 .. 路径穿越与非法字符。
    /// </summary>
    private static string BuildFileName(string reportNumber, string mode)
    {
        string trimmed = (reportNumber ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            throw new ArgumentException("报告号不能为空", nameof(reportNumber));

        if (trimmed is "." or ".." ||
            trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            !string.Equals(Path.GetFileName(trimmed), trimmed, StringComparison.Ordinal))
        {
            throw new ArgumentException("报告号含非法字符或路径分隔符", nameof(reportNumber));
        }

        return $"{trimmed}_{DateTime.Now:yyMMddHHmmss}_{NormalizeMode(mode)}.docx";
    }

    /// <inheritdoc />
    public List<ReportFileMeta> ListReports(string mode, string? keyword)
    {
        string dir = ReportDir;
        if (!Directory.Exists(dir))
            return new List<ReportFileMeta>();

        string normMode = NormalizeMode(mode);
        var result = new List<ReportFileMeta>();
        foreach (string file in Directory.EnumerateFiles(dir, "*.docx"))
        {
            string fileName = Path.GetFileName(file);
            if (TryParseMeta(fileName, file, out var meta) && meta.Mode == normMode)
            {
                if (!string.IsNullOrWhiteSpace(keyword) &&
                    !meta.ReportNumber.Contains(keyword.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                result.Add(meta);
            }
        }

        // 按生成时间倒序（最新在前）
        return result.OrderByDescending(m => m.GeneratedAt).ToList();
    }

    /// <inheritdoc />
    public string? ResolvePath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        // 安全校验: 只允许纯文件名, 拒绝 .. 等路径穿越
        if (Path.GetFileName(fileName) != fileName)
            return null;

        string full = Path.Combine(ReportDir, fileName);
        if (!System.IO.File.Exists(full))
            return null;

        return full;
    }

    /// <summary>模式统一转小写，非法值回退原值（列表时匹配不上即空结果）。</summary>
    private static string NormalizeMode(string mode)
        => string.IsNullOrWhiteSpace(mode) ? string.Empty : mode.Trim().ToLowerInvariant();

    /// <summary>
    /// 从文件名 {报告号}_{yyMMddHHmmss}_{mode}.docx 解析元信息。
    /// 报告号允许含下划线（用首段到倒数第三段拼接），时间戳/模式取固定末两段。
    /// 解析失败（段数不足或时间戳非日期）返回 false。
    /// </summary>
    private static bool TryParseMeta(string fileName, string fullPath, out ReportFileMeta meta)
    {
        meta = new ReportFileMeta { FileName = fileName, GeneratedAt = System.IO.File.GetLastWriteTime(fullPath) };
        if (!fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            return false;

        string name = fileName.Substring(0, fileName.Length - ".docx".Length);
        string[] parts = name.Split('_');
        if (parts.Length < 3)
            return false; // 不是干燥速率报告命名

        meta.Mode = parts[^1].ToLowerInvariant();
        meta.ReportNumber = string.Join('_', parts[..^2]);

        if (DateTime.TryParseExact(parts[^2], "yyMMddHHmmss", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTime generated))
        {
            meta.GeneratedAt = generated;
        }

        meta.SizeBytes = new FileInfo(fullPath).Length;
        return true;
    }
}
