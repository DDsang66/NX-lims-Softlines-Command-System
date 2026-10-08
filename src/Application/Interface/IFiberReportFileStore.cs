using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface;

/// <summary>
/// 纤维成分分析 DOCX 报告文件存储接口。
/// 报告以文件形式存 wwwroot/DocxModel/SaveDocx/FiberAnalysis{yyyyMM}/（2026-09 起按月分目录，
/// 更早的散在 SaveDocx/ 根目录），文件名 = {报告号}_{yyMMddHHmmss}_FiberAnalysis.docx。
/// 前端「历史报告」按钮扫目录列历史报告，由 ListReports/ResolvePath/DeleteReport 支撑。
///
/// 通过 IScopedDependency 自动注册。
/// </summary>
public interface IFiberReportFileStore : IScopedDependency
{
    /// <summary>
    /// 列出历史报告文件的元信息（按生成时间倒序）。
    /// 范围 = SaveDocx 根目录 + 全部 FiberAnalysis* 子目录，都只看本层不递归；
    /// 只认 *_FiberAnalysis.docx。keyword 非空时按报告号子串（不区分大小写）过滤。
    /// </summary>
    List<FiberReportFileMeta> ListReports(string? keyword);

    /// <summary>
    /// 由文件名解析出报告文件的完整路径；文件名不合法（含路径穿越、或不是纤维报告名）
    /// 或文件不存在返回 null。
    /// </summary>
    string? ResolvePath(string fileName);

    /// <summary>
    /// 删除一份历史报告文件（历史列表"删除"按钮）。
    /// 只认报告目录下的纤维报告 .docx 且必须已存在：文件名不合法 / 不是纤维报告 / 文件不在
    /// → 返回 false，不抛异常。物理删除，不可恢复（前端负责二次确认）。
    /// **只删文件，不动库里 fiber_analysis 的行。**
    /// </summary>
    bool DeleteReport(string fileName);
}
