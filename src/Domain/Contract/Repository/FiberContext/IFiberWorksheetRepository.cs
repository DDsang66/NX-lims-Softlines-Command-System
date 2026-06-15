using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.FiberContext
{
    public interface IFiberWorksheetRepository
    {
        /// <summary>
        /// 根据报告号获取工作表
        /// </summary>
        Task<FiberAnalysis?> GetByReportNumberAsync(string reportNumber);

        /// <summary>
        /// 根据ID获取成分分析
        /// </summary>
        Task<FiberAnalysis?> GetByIdAsync(long id, CancellationToken ct);

        /// <summary>
        /// 添加工作表
        /// </summary>
        Task AddAsync(FiberAnalysis worksheet,CancellationToken ct);

        /// <summary>
        /// 更新工作表
        /// </summary>
        Task<FiberAnalysis> UpdateAsync(FiberAnalysis worksheet);

        /// <summary>
        /// 删除工作表
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
    }
}
