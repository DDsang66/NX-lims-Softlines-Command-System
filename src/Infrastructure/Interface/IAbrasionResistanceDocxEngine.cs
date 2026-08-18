using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.AbrasionResistance_FW_;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Interface
{
    public interface IAbrasionResistanceDocxEngine : IScopedDependency
    {
        /// <summary>
        /// 使用 Abrasion Resistance-Rotating Drum Method.docx 模板生成报告
        /// </summary>
        /// <param name="dto">报告数据</param>
        /// <returns>生成的 docx 文件路径</returns>
        void FillReport(string filePath, AbrasionResistanceReportFillModel model);
    }
}
