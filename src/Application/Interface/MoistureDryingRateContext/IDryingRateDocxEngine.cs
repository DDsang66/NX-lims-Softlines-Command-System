using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.MoistureDryingRateContext;

/// <summary>
/// NF5022 报告 docx 填充引擎 — 不透明接口, 不暴露 OpenXml 类型。
/// 按单元格坐标填充 DryingRate.docx 模板, 与成分模板(IWordTemplateEngine)完全隔离。
/// 通过 IScopedDependency 自动注册。
/// </summary>
public interface IDryingRateDocxEngine : IScopedDependency
{
    /// <summary>填充报告: 摘要表 + 6 工位结果表 + 曲线图占位段(模型带 PNG 时嵌入)。整表克隆布局, 保留供回归对照。</summary>
    void FillReport(string filePath, DryingRateReportFillModel model);

    /// <summary>
    /// 填充报告(整页克隆布局): 每个参与组(≤3 工位)独占一整页, 第 2 组起整页克隆并以分页符隔页。
    /// 摘要(报告号/干燥速率均值)/样品名称/Equipment/备注随页重复。报告生成走此方法。
    /// </summary>
    void FillReportByPages(string filePath, DryingRateReportFillModel model);

    /// <summary>
    /// 解析一份历史报告 docx —— 合并报告的数据源(生成时没有另存结构化结果):
    /// 按分页符切页, 每页读回样品名称/干燥速率均值/各样品 m0/滴水量/蒸发曲线/曲线图。
    /// 布局不符或关键值读不出来 → 抛说明性异常。
    /// </summary>
    Nf5022ParsedReport ReadReport(string filePath);

    /// <summary>读历史报告各页的样品名称(历史列表样品名列); 读不出来返回空, 不抛异常。</summary>
    IReadOnlyList<string> ReadSampleNames(string filePath);

    /// <summary>
    /// 填充合并报告: 同一报告号下多份历史报告, 每次测试原样成一页
    /// (每页样品名称/均值原样沿用, 结果网格按本页采样间隔重填), 曲线图全部追加到文末。
    /// </summary>
    void FillCombinedReport(string filePath, DryingRateCombinedReportFillModel model);
}
