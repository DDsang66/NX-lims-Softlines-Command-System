using NX_lims_Softlines_Command_System.Domain.Model.Entities;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository
{
    public interface IFiberDatabaseRepository
    {
        /// <summary>
        /// 获取所有纤维数据
        /// </summary>
        Task<List<FiberDatabase>> GetAllAsync();

        /// <summary>
        /// 根据ID获取纤维数据
        /// </summary>
        Task<FiberDatabase?> GetByIdAsync(Guid id);

        /// <summary>
        /// 根据英文名称获取纤维数据
        /// </summary>
        Task<FiberDatabase?> GetByNameEnAsync(string nameEn);

        /// <summary>
        /// 添加纤维数据
        /// </summary>
        Task<FiberDatabase> AddAsync(FiberDatabase fiber);

        /// <summary>
        /// 更新纤维数据
        /// </summary>
        Task<FiberDatabase> UpdateAsync(FiberDatabase fiber);

        /// <summary>
        /// 删除纤维数据
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 获取所有纤维名称列表（用于前端下拉选择）
        /// </summary>
        Task<List<string>> GetAllNamesAsync();
    }
}
