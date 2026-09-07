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
    /// <summary>填充报告: 摘要表 + 6 工位结果表 + 曲线图占位段(模型带 PNG 时嵌入)。</summary>
    void FillReport(string filePath, DryingRateReportFillModel model);
}
