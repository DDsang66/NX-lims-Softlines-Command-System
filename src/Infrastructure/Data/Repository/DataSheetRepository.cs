using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetBatchContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class DataSheetRepository : IDataSheetRepository, IScopedDependency
    {
        private readonly dbContext _context;

        public DataSheetRepository(dbContext context)
        {
            _context = context;
        }

        /* ============================================================
         * 写
         * ============================================================ */

        public async Task AddAsync(src.Domain.Aggregeates.DataSheetContext.DataSheet aggregateRoot, CancellationToken ct)
        {
            if (aggregateRoot == null)
                throw new ArgumentNullException(nameof(aggregateRoot));

            var po = ToPo(aggregateRoot);
            await _context.Set<Persistence.DataSheet>().AddAsync(po, ct);
        }

        public async Task UpdateAsync(src.Domain.Aggregeates.DataSheetContext.DataSheet aggregateRoot, CancellationToken ct)
        {
            if (aggregateRoot == null)
                throw new ArgumentNullException(nameof(aggregateRoot));

            // 先查再改，避免覆盖 CreateTime 等不该动的列
            var existing = await _context.Set<Persistence.DataSheet>()
                .FirstOrDefaultAsync(x => x.Id == aggregateRoot.Id, ct);

            if (existing == null)
                throw new InvalidOperationException(
                    $"DataSheet {aggregateRoot.Id} not found");

            existing.BatchId = aggregateRoot.BatchId;
            existing.CheckListId = aggregateRoot.CheckListId;
            existing.TestItemId = aggregateRoot.TestItemId;   // string
            existing.ReportNumber = aggregateRoot.ReportNumber;
            existing.Url = aggregateRoot.Url ?? string.Empty;
            existing.ContactTemplateUrl = aggregateRoot.ContactTemplateUrl;
            existing.Status = (int)aggregateRoot.Status;
            existing.ModelIndex = aggregateRoot.ModelIndex;
            existing.ModelKey = aggregateRoot.ModelKey;
            existing.ModelSnapshot = aggregateRoot.ModelSnapshot;
            existing.ErrorMessage = aggregateRoot.ErrorMessage;
            existing.RetryCount = aggregateRoot.RetryCount;
            existing.Version = aggregateRoot.Version;
            existing.UpdateTime = aggregateRoot.UpdateTime;
            // CreateTime 不覆盖
        }

        /* ============================================================
         * 读
         * ============================================================ */

        public async Task<src.Domain.Aggregeates.DataSheetContext.DataSheet?> GetByIdAsync(DataSheetId aggregateRootId, CancellationToken ct)
        {
            var po = await _context.Set<Persistence.DataSheet>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == aggregateRootId.Value, ct);

            return po == null ? null : ToDomain(po);
        }

        public async Task<List<src.Domain.Aggregeates.DataSheetContext.DataSheet>> GetPendingAsync(int batchSize, CancellationToken ct)
        {
            var pos = await _context.Set<Persistence.DataSheet>()
                .AsNoTracking()
                .Where(x => x.Status == (int)DataSheetStatus.Pending)
                .OrderBy(x => x.CreateTime)
                .Take(batchSize)
                .ToListAsync(ct);

            return pos.Select(ToDomain).ToList();
        }
        
        public async Task<List<src.Domain.Aggregeates.DataSheetContext.DataSheet>> GetByCheckListIdAsync(
            CheckListId checkListId, CancellationToken ct)
        {
            var pos = await _context.Set<Persistence.DataSheet>()
                .AsNoTracking()
                .Where(x => x.CheckListId == checkListId.Value)
                .OrderBy(x => x.CreateTime)
                .ToListAsync(ct);

            return pos.Select(ToDomain).ToList();
        }

        public async Task<bool> ExistsAsync(
            CheckListId checkListId, string testItemId, int modelIndex, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(testItemId))
                return false;

            return await _context.Set<Persistence.DataSheet>()
                .AsNoTracking()
                .AnyAsync(x => x.CheckListId == checkListId.Value
                            && x.TestItemId == testItemId
                            && x.ModelIndex == modelIndex, ct);
        }

        public async Task<src.Domain.Aggregeates.DataSheetContext.DataSheet?> GetByIndexAsync(
            CheckListId checkListId, string testItemId, int modelIndex, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(testItemId))
                return null;

            var po = await _context.Set<Persistence.DataSheet>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CheckListId == checkListId.Value
                                       && x.TestItemId == testItemId
                                       && x.ModelIndex == modelIndex, ct);

            return po == null ? null : ToDomain(po);
        }

        /* ============================================================
         * 映射
         * ============================================================ */

        private static Persistence.DataSheet ToPo(src.Domain.Aggregeates.DataSheetContext.DataSheet ds)
        {
            return new Persistence.DataSheet
            {
                Id = ds.Id.Value,
                BatchId = ds.BatchId.Value,
                CheckListId = ds.CheckListId.Value,
                TestItemId = ds.TestItemId.Value,        // string
                ReportNumber = ds.ReportNumber,
                Url = ds.Url ?? string.Empty,
                ContactTemplateUrl = ds.ContactTemplateUrl,
                Status = (int)ds.Status,
                ModelIndex = ds.ModelIndex,
                ModelKey = ds.ModelKey,
                ModelSnapshot = ds.ModelSnapshot,
                ErrorMessage = ds.ErrorMessage,
                RetryCount = ds.RetryCount,
                Version = ds.Version,
                CreateTime = ds.CreateTime,
                UpdateTime = ds.UpdateTime
            };
        }

        private static src.Domain.Aggregeates.DataSheetContext.DataSheet ToDomain(Persistence.DataSheet po)
        {
            return src.Domain.Aggregeates.DataSheetContext.DataSheet.Rebuild(
                id: new DataSheetId(po.Id),
                checkListId: new CheckListId(po.CheckListId),
                batchId: new DataSheetBatchId(po.BatchId),
                testItemId: new TestItemId(po.TestItemId),   // string
                reportNumber: po.ReportNumber,
                url: po.Url,
                contactTemplateUrl: po.ContactTemplateUrl,
                status: (DataSheetStatus)po.Status,
                modelIndex: po.ModelIndex,
                modelKey: po.ModelKey,
                modelSnapshot: po.ModelSnapshot,
                errorMessage: po.ErrorMessage,
                retryCount: po.RetryCount,
                version: po.Version,
                createTime: po.CreateTime,
                updateTime: po.UpdateTime);
        }
    }
}
