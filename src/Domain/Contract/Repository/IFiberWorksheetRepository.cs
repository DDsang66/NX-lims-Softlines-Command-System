using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository
{
    public interface IFiberWorksheetRepository
    {
        /// <summary>
        /// 根据报告号获取工作表
        /// </summary>
        Task<FiberWorksheet?> GetByReportNumberAsync(string reportNumber);

        /// <summary>
        /// 根据ID获取工作表（包含明细和结果）
        /// </summary>
        Task<FiberWorksheet?> GetByIdWithDetailsAsync(Guid id);

        /// <summary>
        /// 添加工作表
        /// </summary>
        Task<FiberWorksheet> AddAsync(FiberWorksheet worksheet);

        /// <summary>
        /// 更新工作表
        /// </summary>
        Task<FiberWorksheet> UpdateAsync(FiberWorksheet worksheet);

        /// <summary>
        /// 删除工作表
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
    }
}
