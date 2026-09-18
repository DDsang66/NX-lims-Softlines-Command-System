using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.YarnCountContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Interface
{
    /// <summary>纱支 docx 填充引擎 — 用 PHY_YarnCount.docx 模板填报告</summary>
    public interface IYarnCountDocxEngine : IScopedDependency
    {
        /// <summary>就地填充已复制的模板文件。模板结构与坐标假设不符时抛异常(不静默出错位文档)</summary>
        void FillReport(string filePath, YarnCountReportFillModel model);
    }
}
