using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface;

/// <summary>
/// 干燥速率 DOCX 报告文件存储接口。
/// 测试记录不落结构化库，报告以文件形式存服务器独立子目录
/// wwwroot/DocxModel/SaveDocx/DryingRate/。
/// 文件名 = {报告号}_{yyMMddHHmmss}_{mode}.docx，保证文件不撞名、列表可解析。
/// 前端「数据查询」改为历史报告文件列表下载，由 ListReports/ResolvePath 支撑。
/// 通过 IScopedDependency 自动注册。
/// </summary>
public interface IReportFileStore : IScopedDependency
{
    /// <summary>
    /// 保存报告内容到报告目录，返回生成的文件名。
    /// </summary>
    string SaveReport(byte[] content, string reportNumber, string mode);

    /// <summary>
    /// 复制模板为报告文件（{报告号}_{yyMMddHHmmss}_{mode}.docx）并返回完整路径,
    /// 供报告引擎就地填充（填充后即最终报告文件）。
    /// 模板不存在抛 InvalidOperationException（带模板相对路径, 便于排查）。
    /// </summary>
    string PrepareReportFile(string templateRelativePath, string reportNumber, string mode);

    /// <summary>
    /// 按模式 + 报告号关键字列出历史报告文件元信息（按生成时间倒序）。
    /// </summary>
    List<ReportFileMeta> ListReports(string mode, string? keyword);

    /// <summary>
    /// 由文件名解析出报告文件的完整路径；文件名不合法（含路径穿越等）或文件不存在返回 null。
    /// </summary>
    string? ResolvePath(string fileName);

    /// <summary>
    /// 删除一份历史报告文件（历史列表"删除"按钮）。
    /// 只认报告目录下的 .docx 且必须已存在：文件名不合法 / 不是 docx / 文件不在 → 返回 false，不抛异常。
    /// 物理删除，不可恢复（前端负责二次确认）。
    /// </summary>
    bool DeleteReport(string fileName);
}
