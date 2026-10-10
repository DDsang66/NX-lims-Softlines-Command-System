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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var outbox = scope.ServiceProvider.GetRequiredService<IEventOutbox>();
                    var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                    var events = await outbox.GetUnpublishedEventsAsync(100, stoppingToken);

                    foreach (var @event in events)
                    {
                        try
                        {
                            await mediator.Publish(@event, stoppingToken);
                            await outbox.MarkAsPublishedAsync(@event.EventId, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to publish event {EventId}", @event.EventId);

                            await outbox.IncrementRetryAsync(@event.EventId, ex.Message, stoppingToken);

                            var entry = await outbox.GetEntryAsync(@event.EventId, stoppingToken);
                            if (entry != null && entry.RetryCount >= 3)
                            {
                                await outbox.MarkAsDeadLetterAsync(@event.EventId, stoppingToken);
                                _logger.LogError("Event {EventId} moved to dead letter", @event.EventId);
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // 后台服务不能因为单轮异常就退出
                    _logger.LogError(ex, "Error in EventPublisherBackgroundService loop");
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
