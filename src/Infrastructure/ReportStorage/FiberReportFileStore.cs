// ============================================================
// 纤维成分分析报告文件存储 (FiberReportFileStore)
//
// 职责: 把 wwwroot/DocxModel/SaveDocx/ 下**已经生成好的**纤维报告 docx 列出来,
//       支撑前端「历史报告」弹窗的 列表 / 下载 / 删除。
//       只读 + 删除, **不参与生成** —— 生成侧仍走 FiberWorksheetService.BuildAnalysisAsync +
//       IFileStorageService.CopyTemplate。
//
// 目录布局(与生成侧共用 FiberWorksheetService.MonthlyFolder()):
//   SaveDocx/FiberAnalysis{yyyyMM}/{报告号}_{yyMMddHHmmss}_FiberAnalysis.docx   ← 2026-09 起按月归档
//   SaveDocx/{报告号}_{yyMMddHHmmss}_FiberAnalysis.docx                        ← 归档前的老报告
//   文件名末段恒为 FiberAnalysis, 所以它**只能当校验键, 不能当过滤键** —— 这也是本接口
//   没有 mode 参数的原因(干燥速率那边末段是 nf5022/aatcc201, 是要按它过滤的)。
// ============================================================
using System.Globalization;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Application.Service;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.ReportStorage;

/// <summary>
/// 纤维报告文件存储实现。
/// 注入 IWebHostEnvironment 定位 wwwroot; 报告目录**不在这里创建**(生成侧负责建),
/// 目录不存在时列表返回空、定位返回 null。
/// </summary>
public class FiberReportFileStore : IFiberReportFileStore, IScopedDependency
{
    /// <summary>报告文件名末段 —— 同时是校验键: 只认以它结尾的 docx</summary>
    private const string ReportSuffix = "_FiberAnalysis.docx";

    /// <summary>月度子目录前缀, 与 FiberWorksheetService.MonthlyFolder() 同源</summary>
    private const string MonthFolderPrefix = "FiberAnalysis";

    private readonly IWebHostEnvironment _env;

    public FiberReportFileStore(IWebHostEnvironment env)
    {
        _env = env;
    }

    /// <summary>报告根目录完整路径（wwwroot/DocxModel/SaveDocx）</summary>
    private string SaveDocxDir => Path.Combine(_env.WebRootPath, "DocxModel", "SaveDocx");

    /// <inheritdoc />
    public List<FiberReportFileMeta> ListReports(string? keyword)
    {
        string root = SaveDocxDir;
        // 目录不存在就直接空 — 否则 EnumerateDirectories 会抛 DirectoryNotFoundException
        if (!Directory.Exists(root))
            return new List<FiberReportFileMeta>();

        string? kw = string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim();

        var result = new List<FiberReportFileMeta>();
        // 根目录(老报告) + 每个 FiberAnalysis{yyyyMM} 子目录, 都只看本层不递归
        foreach (string dir in EnumerateReportDirs(root))
        {
            foreach (string file in Directory.EnumerateFiles(dir, "*.docx", SearchOption.TopDirectoryOnly))
            {
                // 单个文件读不出元信息(解析失败/枚举与读取之间被删)只跳过它,
                // 不能让整个列表接口打不开
                if (!TryParseMeta(file, out var meta)) continue;
                if (kw != null && !meta.ReportNumber.Contains(kw, StringComparison.OrdinalIgnoreCase)) continue;
                result.Add(meta);
            }
        }

        // 按生成时间倒序（最新在前）
        return result.OrderByDescending(m => m.GeneratedAt).ToList();
    }

    /// <inheritdoc />
    public string? ResolvePath(string fileName)
    {
        if (!IsFiberReportFileName(fileName))
            return null;

        string root = SaveDocxDir;

        // 1. 当月目录 —— 调生成侧同一个 MonthlyFolder(), 别在这儿另算月份
        string monthly = Path.Combine(root, FiberWorksheetService.MonthlyFolder(), fileName);
        if (File.Exists(monthly)) return monthly;

        // 2. 根目录 —— 2026-09 按月归档之前生成的老报告
        string legacy = Path.Combine(root, fileName);
        if (File.Exists(legacy)) return legacy;

        // 3. 兜底扫其他月份目录 —— 兼容跨月情况。
        //    刻意不用 AllDirectories: 那会把 DryingRate/Weight/YarnCount 的报告一起扫进来,
        //    接上删除就是跨模块误删。合法纤维报告只可能在 FiberAnalysis* 或根目录。
        if (!Directory.Exists(root))
            return null;

        foreach (string dir in Directory.EnumerateDirectories(root, MonthFolderPrefix + "*", SearchOption.TopDirectoryOnly))
        {
            string candidate = Path.Combine(dir, fileName);
            if (File.Exists(candidate)) return candidate;
        }

        return null;
    }

    /// <inheritdoc />
    public bool DeleteReport(string fileName)
    {
        // ResolvePath 已挡住路径穿越、非纤维报告名, 并确认文件存在且只落在报告目录下;
        // 加扩展名校验是防手滑——目录按约定只放报告 docx, 别的一律不删。
        string? path = ResolvePath(fileName);
        if (path == null || !path.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
            return false;

        File.Delete(path);
        return true;
    }

    /// <summary>报告可能落地的目录: SaveDocx 根 + 全部 FiberAnalysis* 子目录(含历史月份)。</summary>
    private static IEnumerable<string> EnumerateReportDirs(string root)
    {
        yield return root;

        foreach (string dir in Directory.EnumerateDirectories(root, MonthFolderPrefix + "*", SearchOption.TopDirectoryOnly))
            yield return dir;
    }

    /// <summary>
    /// 文件名是否合法且确实是纤维报告。两条都要过:
    /// 纯文件名(挡 .. 路径穿越) + 以 _FiberAnalysis.docx 结尾(挡别模块的报告)。
    /// 大小写不敏感, 将来换 Linux 部署时行为一致。
    /// </summary>
    private static bool IsFiberReportFileName(string? fileName)
        => !string.IsNullOrWhiteSpace(fileName)
           && Path.GetFileName(fileName) == fileName
           && fileName.EndsWith(ReportSuffix, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 从文件名 {报告号}_{yyMMddHHmmss}_FiberAnalysis.docx 解析元信息。
    /// 报告号允许含下划线(首段到倒数第二段拼接), 时间戳取固定末段。
    /// 非纤维报告名 / 段数不足 / 读取抛异常 → 返回 false(跳过该文件)。
    /// </summary>
    private static bool TryParseMeta(string fullPath, out FiberReportFileMeta meta)
    {
        meta = new FiberReportFileMeta();

        try
        {
            string fileName = Path.GetFileName(fullPath);
            if (!IsFiberReportFileName(fileName))
                return false;

            string name = fileName[..^ReportSuffix.Length];
            string[] parts = name.Split('_');
            if (parts.Length < 2)
                return false; // 报告号为空 / 没有时间戳段 —— 不是本模块的命名

            meta.FileName = fileName;
            meta.ReportNumber = string.Join('_', parts[..^1]);
            meta.GeneratedAt = DateTime.TryParseExact(parts[^1], "yyMMddHHmmss", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateTime generated)
                ? generated
                : File.GetLastWriteTime(fullPath);   // 时间戳段解析不出就不认死, 退回文件修改时间
            meta.SizeBytes = new FileInfo(fullPath).Length;
            return true;
        }
        catch
        {
            // 枚举到与实际读取之间文件被删/被占用 → 跳过这一份, 别让整个列表失败
            return false;
        }
    }
}
