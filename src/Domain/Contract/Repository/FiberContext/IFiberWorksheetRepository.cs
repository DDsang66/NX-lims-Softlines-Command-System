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
        /// 根据报告号取**全部**录入行（按 created_at 倒序）。
        /// 每次 Build Analysis / Save a Draft 都是新插一行，同一报告号会有多行；
        /// 历史报告回填靠"文件时间戳"在候选行里挑最接近的一条，故这里不能只返回一条。
        /// </summary>
        Task<List<FiberAnalysis>> GetAllByReportNumberAsync(string reportNumber);

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
