using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.MoistureDryingRateContext;

/// <summary>
/// AATCC 201 报告 docx 填充引擎 — 不透明接口, 不暴露 OpenXml 类型。
/// 按单元格坐标填充 Aatcc201.docx 模板, 与成分模板(IWordTemplateEngine)完全隔离。
/// 通过 IScopedDependency 自动注册。
/// </summary>
public interface IAatcc201DocxEngine : IScopedDependency
{
    /// <summary>填充单样品报告: 摘要表 + 第一张结果表 + 曲线图(模型带 PNG 时追加文末)。</summary>
    void FillReport(string filePath, Aatcc201ReportFillModel model);

    /// <summary>
    /// 填充合并报告: 同一报告号下多个样品, 一个样品一张 Sample 表（多于模板自带 3 张时克隆空白表），
    /// 曲线图全部追加到文档末尾。
    /// </summary>
    void FillCombinedReport(string filePath, Aatcc201CombinedReportFillModel model);

    /// <summary>
    /// 解析一份历史报告 docx, 读回可再填内容（报告号/样品块/曲线图/页脚温湿度）。
    /// 数据源是文件本身(生成时刻未落结构化结果); 结构不符时抛描述性异常。
    /// </summary>
    Aatcc201ParsedReport ReadReport(string filePath);

    /// <summary>
    /// 只读该报告里的样品名称（历史列表样品名列用; 廉价, 不读曲线图）。
    /// 任何单文件结构不符/读取失败 → 返回空列表, 不让一个坏文件拖垮整个列表。
    /// </summary>
    IReadOnlyList<string> ReadSampleNames(string filePath);
}
