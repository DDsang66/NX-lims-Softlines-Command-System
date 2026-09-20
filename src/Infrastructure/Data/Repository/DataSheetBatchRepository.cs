using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetBatchContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetBatchContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class DataSheetBatchRepository : IDataSheetBatchRepository, IScopedDependency
    {
        private readonly dbContext _context;

        public DataSheetBatchRepository(dbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 添加批次
        /// </summary>
        public async Task AddAsync(src.Domain.Aggregeates.DataSheetBatchContext.DataSheetBatch batch, CancellationToken ct)
        {
            if (batch == null) throw new ArgumentNullException(nameof(batch));

            var po = ToPo(batch);
            await _context.Set<Persistence.DataSheetBatch>().AddAsync(po, ct);
        }

        /// <summary>
        /// 按 Id 查批次
        /// </summary>
        public async Task<src.Domain.Aggregeates.DataSheetBatchContext.DataSheetBatch?> GetByIdAsync(DataSheetBatchId id, CancellationToken ct)
        {
            var po = await _context.Set<Persistence.DataSheetBatch>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id.Value, ct);

            return po == null ? null : ToDomain(po);
        }

        /// <summary>
        /// 查某 checklist 下「未失败」的活跃批次（用于幂等）
        /// </summary>
        public async Task<src.Domain.Aggregeates.DataSheetBatchContext.DataSheetBatch?> GetActiveByCheckListIdAsync(
            CheckListId checkListId, CancellationToken ct)
        {
            // 1. 先查该 checklist 下所有 datasheet 的状态
            var hasUnfinished = await _context.Set<Persistence.DataSheet>()
                .AsNoTracking()
                .AnyAsync(x => x.CheckListId == checkListId.Value
                            && (x.Status == (int)DataSheetStatus.Pending
                             || x.Status == (int)DataSheetStatus.Generating), ct);

            if (!hasUnfinished)
                return null;   // 全部到终态，不算活跃

            // 2. 有未完成的，返回最近一个 batch
            var po = await _context.Set<Persistence.DataSheetBatch>()
                .AsNoTracking()
                .Where(x => x.CheckListId == checkListId.Value)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync(ct);

            return po == null ? null : ToDomain(po);
        }

        /// <summary>
        /// 更新批次
        /// </summary>
        public async Task UpdateAsync(
            src.Domain.Aggregeates.DataSheetBatchContext.DataSheetBatch batch,
            CancellationToken ct)
        {
            if (batch == null) throw new ArgumentNullException(nameof(batch));

            // 方式 A：Attach + Update（简单，但会覆盖所有列）
            var po = ToPo(batch);
            _context.Set<Persistence.DataSheetBatch>().Update(po);

             var existing = await _context.Set<Persistence.DataSheetBatch>()
                 .FirstOrDefaultAsync(x => x.Id == batch.Id.Value, ct);
            if (existing == null) throw new InvalidOperationException($"Batch {batch.Id.Value} not found");

            existing.ReportNo = batch.ReportNo;
            existing.Total = batch.Total;
            existing.Status = (int)batch.Status;
            existing.TemplateUrl = batch.TemplateUrl;
            existing.MergedPdfUrl = batch.MergedPdfUrl;
            existing.UpdatedAt = batch.UpdatedAt;
            // CreatedAt 不覆盖
        }

        /// <summary>
        /// 查某 checklist 下所有批次
        /// </summary>
        public async Task<List<src.Domain.Aggregeates.DataSheetBatchContext.DataSheetBatch>> GetByCheckListIdAsync(
            CheckListId checkListId, CancellationToken ct)
        {
            var pos = await _context.Set<Persistence.DataSheetBatch>()
                .AsNoTracking()
                .Where(x => x.CheckListId == checkListId.Value)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync(ct);

            return pos.Select(ToDomain).ToList();
        }

        /* ============================================================
         * 映射
         * ============================================================ */

        private static Persistence.DataSheetBatch ToPo(src.Domain.Aggregeates.DataSheetBatchContext.DataSheetBatch batch)
        {
            return new Persistence.DataSheetBatch
            {
                Id = batch.Id.Value,
                CheckListId = batch.CheckListId.Value,
                ReportNo = batch.ReportNo,
                Total = batch.Total,
                Status = (int)batch.Status,
                TemplateUrl = batch.TemplateUrl,
                MergedPdfUrl = batch.MergedPdfUrl,
                CreatedAt = batch.CreatedAt,
                UpdatedAt = batch.UpdatedAt
            };
        }

        private static src.Domain.Aggregeates.DataSheetBatchContext.DataSheetBatch ToDomain(Persistence.DataSheetBatch po)
        {
            // 你的 DataSheetBatch 没有 Rebuild 方法，
            // 只有 Create。Create 会重置 CreatedAt / UpdatedAt / Status。
            // 所以这里需要 DataSheetBatch 暴露一个 Rebuild，
            // 或者用反射/构造函数。
            // 
            // 如果暂时没有 Rebuild，需要先给 DataSheetBatch 加一个。
            return src.Domain.Aggregeates.DataSheetBatchContext.DataSheetBatch.Rebuild(
                id: new DataSheetBatchId(po.Id),
                checkListId: new CheckListId(po.CheckListId),
                reportNo: po.ReportNo,
                total: po.Total,
                status: (DataSheetBatchStatus)po.Status,
                templateUrl: po.TemplateUrl,
                mergedPdfUrl: po.MergedPdfUrl,
                createdAt: po.CreatedAt,
                updatedAt: po.UpdatedAt);
        }
    }
}
