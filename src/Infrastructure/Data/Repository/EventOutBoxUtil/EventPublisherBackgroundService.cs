using MediatR;
using Microsoft.Extensions.Logging;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository.EventOutBoxUtil
{
    public class EventPublisherBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EventPublisherBackgroundService> _logger;

        public EventPublisherBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<EventPublisherBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// 每隔 5 秒检查未发布的事件
        /// 通过 MediatR 找到对应的 Handler 执行
        /// 失败时重试，超过 3 次标记为死信
        /// 事件发布与业务请求分离，避免阻塞
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var outbox = scope.ServiceProvider.GetRequiredService<IEventOutbox>();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var processedEventTracker = scope.ServiceProvider.GetRequiredService<IProcessedEventTracker>(); 

              var events = await outbox.GetUnpublishedEventsAsync(100, stoppingToken);

                foreach (var @event in events)
                {
                    try
                    {
                        // 1. 【新增】幂等性检查：如果已经处理过，则跳过
                        if (await processedEventTracker.IsProcessedAsync(@event.EventId, stoppingToken))
                        {
                            _logger.LogWarning("Event {EventId} has already been processed. Skipping.", @event.EventId);
                            // 可选：如果已经处理过，但 Outbox 中还是未发布状态，可以在这里直接标记为已发布
                            await outbox.MarkAsPublishedAsync(@event.EventId, stoppingToken);

                            continue;
                        }

                        await mediator.Publish(@event, stoppingToken);

                        await processedEventTracker.MarkAsProcessedAsync(@event.EventId, stoppingToken);

                        //可以更改为批量标记已发布
                        await outbox.MarkAsPublishedAsync(@event.EventId, stoppingToken);

                        //可能会有并发冲突，导致重复发布事件，使用日志记录
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to publish event {EventId}", @event.EventId);

                        // 递增重试次数
                        await outbox.IncrementRetryAsync(@event.EventId, ex.Message, stoppingToken);

                        // 超过 3 次，标记为死信
                        var entry = await outbox.GetEntryAsync(@event.EventId, stoppingToken);
                        if (entry != null && entry.RetryCount >= 3)
                        {
                            await outbox.MarkAsDeadLetterAsync(@event.EventId, stoppingToken);
                            _logger.LogError("Event {EventId} moved to dead letter", @event.EventId);
                        }
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}
