using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.PhysicalWeightContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.PhysicalWeightContext
{
    /// <summary>
    /// 物理克重 docx 填充引擎 — 不透明接口, 不暴露 OpenXml 类型。
    /// 按单元格坐标填充 PHY_Weight.docx 模板, 与成分模板(IWordTemplateEngine)完全隔离。
    /// </summary>
    public interface IPhysicalWeightDocxEngine : IScopedDependency
    {
        /// <summary>填充报告: 表0(报告号/方法/汇总) + 表1(数据行)</summary>
        void FillReport(string filePath, PhysicalWeightReportFillModel model);
    }
}
