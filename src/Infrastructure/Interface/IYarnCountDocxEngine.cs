using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.YarnCountContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Interface
{
    /// <summary>
    /// 纱支 docx 填充引擎 — 按 model.FabricType 选坐标填报告
    /// (PHY_YarnCount_woven.docx / PHY_YarnCount_knit.docx, 模板文件由服务端挑好并传进来)
    /// </summary>
    public interface IYarnCountDocxEngine : IScopedDependency
    {
        /// <summary>
        /// 就地填充已复制的模板文件。未知布种、或模板结构与坐标假设不符时抛异常(不静默出错位文档)。
        /// 传进来的模板必须与 model.FabricType 对得上 —— 拿 knit 的坐标填 woven 模板会当场抛。
        /// </summary>
        void FillReport(string filePath, YarnCountReportFillModel model);
    }
}
