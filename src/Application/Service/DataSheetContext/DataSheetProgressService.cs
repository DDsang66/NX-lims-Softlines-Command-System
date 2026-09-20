using Microsoft.Extensions.Caching.Distributed;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.DataSheetConetxt;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetBatchContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.DataSheetContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Application.Service.DataSheetContext
{
    public class DataSheetProgressService : IScopedDependency
    {
        private readonly IDataSheetRepository _dsRepo;
        private readonly IDataSheetBatchRepository _batchRepo;
        private readonly IDistributedCache _cache;

        public DataSheetProgressService(
            IDataSheetRepository dsRepo,
            IDataSheetBatchRepository batchRepo,
            IDistributedCache cache)
        {
            _dsRepo = dsRepo;
            _batchRepo = batchRepo;
            _cache = cache;
        }
        /// <summary>
        /// Get snapshot of data sheet progress
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<DataSheetProgressDto> GetSnapshot(
            CheckListId checkListId, CancellationToken ct)
        {
            var key = $"datasheet:progress:{checkListId.Value}";
            var cached = await _cache.GetStringAsync(key, ct);
            if (cached != null)
                return JsonSerializer.Deserialize<DataSheetProgressDto>(cached)!;

            var items = await _dsRepo.GetByCheckListIdAsync(checkListId, ct);

            var batches = await _batchRepo.GetByCheckListIdAsync(checkListId, ct);

            var latestBatch = batches.OrderByDescending(b => b.CreatedAt).FirstOrDefault();

            var dto = new DataSheetProgressDto
            {
                CheckListId = checkListId.Value,
                BatchId = latestBatch?.Id.Value ?? Guid.Empty,
                Total = items.Count,
                Success = items.Count(i => i.Status == DataSheetStatus.Created),
                Failed = items.Count(i => i.Status == DataSheetStatus.Failed),
                Generating = items.Count(i => i.Status == DataSheetStatus.Generating),
                Pending = items.Count(i => i.Status == DataSheetStatus.Pending),
                Status = ComputeOverallStatus(items, latestBatch),
                MergedPdfUrl = latestBatch?.MergedPdfUrl,
                Items = items.Select(ToItemDto).ToList()
            };

            await _cache.SetStringAsync(
                key,
                JsonSerializer.Serialize(dto),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(0.5)
                },
                ct);

            return dto;
        }

        /// <summary>
        /// Compute overall status of data sheet batch
        /// </summary>
        /// <param name="items"></param>
        /// <param name="batch"></param>
        /// <returns></returns>
        private static string ComputeOverallStatus(
            List<DataSheet> items, DataSheetBatch? batch)
        {
            if (items.Count == 0) return "PENDING";
            var success = items.Count(i => i.Status == DataSheetStatus.Created);
            var failed = items.Count(i => i.Status == DataSheetStatus.Failed);
            var done = success + failed;

            if (done < items.Count)
                return "GENERATING";

            if (failed == 0) 
                return "SUCCESS";

            if (success == 0)
                return "FAILED";

            return "PARTIAL_FAILED";
        }

        /// <summary>
        /// DTO转换逻辑
        /// </summary>
        /// <param name="ds"></param>
        /// <returns></returns>
        private static DataSheetItemDto ToItemDto(DataSheet ds)
        {
            return new DataSheetItemDto
            {
                ProjectId = ds.TestItemId ?? "",
                TestItemId = ds.TestItemId ?? "",
                DataSheetId = ds.Id.Value.ToString(),
                ModelIndex = ds.ModelIndex,
                ModelKey = ds.ModelKey,
                Status = ds.Status.ToString().ToUpperInvariant(),
                FileUrl = ds.Url,
                ErrorMessage = ds.ErrorMessage,
                RetryCount = ds.RetryCount,
                UpdatedAt = ds.UpdateTime.HasValue ? ds.UpdateTime.Value : DateTime.Now
            };
        }
    }
}
