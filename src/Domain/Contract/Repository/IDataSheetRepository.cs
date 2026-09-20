using NX_lims_Softlines_Command_System.Domain;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;
using System.Runtime.CompilerServices;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository
{
    public interface IDataSheetRepository : IRepository<DataSheet, DataSheetId, Guid>,IScopedDependency
    {
        /// <summary>
        /// 添加聚合根
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <returns></returns>
        Task AddAsync(DataSheet aggregateRoot, CancellationToken ct);

        /// <summary>
        /// 修改聚合根
        /// </summary>
        /// <param name="aggregateRoot"></param>
        /// <returns></returns>
        Task UpdateAsync(DataSheet aggregateRoot, CancellationToken ct);

        /// <summary>
        /// 查询聚合根
        /// </summary>
        /// <param name="aggregateRootId"></param>
        /// <param name="ct"></param>
        /// <returns>聚合根</returns>
        Task<DataSheet> GetByIdAsync(DataSheetId aggregateRootId, CancellationToken ct);

        /// <summary>
        /// Get pending data sheets
        /// </summary>
        /// <param name="batchSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<List<DataSheet>> GetPendingAsync(int batchSize, CancellationToken ct);

        /// <summary>
        /// Get data sheets by check list id
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<List<DataSheet>> GetByCheckListIdAsync(CheckListId checkListId, CancellationToken ct);

        /// <summary>
        /// 根据checkListId, testItemId, modelIndex查询是否存在
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="testItemId"></param>
        /// <param name="modelIndex"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<bool> ExistsAsync(CheckListId checkListId, string testItemId, int modelIndex, CancellationToken ct);

        /// <summary>
        /// 根据checkListId, testItemId, modelIndex查询
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="testItemId"></param>
        /// <param name="modelIndex"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<DataSheet?> GetByIndexAsync(CheckListId checkListId, string testItemId, int modelIndex, CancellationToken ct);
    }
}
