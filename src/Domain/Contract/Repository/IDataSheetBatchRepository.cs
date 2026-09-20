using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetBatchContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetBatchContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository
{
    public interface IDataSheetBatchRepository : IScopedDependency, IRepository<DataSheetBatch, DataSheetBatchId, Guid>
    {
        /// <summary>
        /// 添加批次
        /// </summary>
        /// <param name="batch"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task AddAsync(DataSheetBatch batch, CancellationToken ct);

        /// <summary>
        /// 更新批次
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<DataSheetBatch?> GetByIdAsync(DataSheetBatchId id, CancellationToken ct);

        /// <summary>
        /// 获取批次
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<DataSheetBatch?> GetActiveByCheckListIdAsync(CheckListId checkListId, CancellationToken ct);

        /// <summary>
        /// 获取批次
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<List<DataSheetBatch>> GetByCheckListIdAsync(CheckListId checkListId, CancellationToken ct);
    }
}
