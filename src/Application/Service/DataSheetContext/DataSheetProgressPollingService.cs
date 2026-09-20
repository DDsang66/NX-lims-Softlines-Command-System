using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.DataSheetConetxt;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.DataSheetContext
{
    public class DataSheetProgressPollingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly DataSheetProgressHub _hub;
        private readonly ILogger<DataSheetProgressPollingService> _logger;

        public DataSheetProgressPollingService(
            IServiceScopeFactory scopeFactory,
            DataSheetProgressHub hub,
            ILogger<DataSheetProgressPollingService> logger)
        {
            _scopeFactory = scopeFactory;
            _hub = hub;
            _logger = logger;
        }

        /// <summary>
        /// 每秒轮询一次
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.LogInformation("DataSheetProgressPollingService started");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var activeIds = _hub.GetActiveCheckListIds();

                    foreach (var checkListId in activeIds)
                    {
                        using var scope = _scopeFactory.CreateScope();
                        var progressService = scope.ServiceProvider
                            .GetRequiredService<DataSheetProgressService>();

                        var snapshot = await progressService.GetSnapshot(
                            new CheckListId(checkListId), ct);

                        if (!_hub.HasChanged(checkListId, snapshot))
                            continue;

                        await _hub.Broadcast(checkListId, "progress", snapshot);

                        // 全部完成 → 推 completed + 关连接
                        if (IsCompleted(snapshot))
                        {
                            await _hub.Broadcast(checkListId, "completed", snapshot);
                            _hub.CloseAll(checkListId);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DataSheetProgressPollingService error");
                }

                await Task.Delay(1000, ct);
            }
        }

        /// <summary>
        /// 判断是否全部完成
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        private static bool IsCompleted(DataSheetProgressDto dto)
        {
            return dto.Total > 0
                && (dto.Status == "SUCCESS"
                 || dto.Status == "FAILED"
                 || dto.Status == "PARTIAL_FAILED");
        }
    }
}
