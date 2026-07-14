using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository.EventOutBoxUtil
{
    public class ProcessedEventTracker : IProcessedEventTracker,IScopedDependency
    {
        private readonly dbContext _dbContext;

        public ProcessedEventTracker(dbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 检测这个事件是否已经处理过
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<bool> IsProcessedAsync(Guid eventId, CancellationToken ct)
        {
            return await _dbContext.Set<ProcessedEvent>()
                .AnyAsync(e => e.EventId == eventId, ct);
        }

        /// <summary>
        /// 标记这个事件已经处理过
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task MarkAsProcessedAsync(Guid eventId, CancellationToken ct)
        {
            await _dbContext.Set<ProcessedEvent>().AddAsync(new ProcessedEvent
            {
                EventId = eventId,
                ProcessedAt = DateTime.UtcNow
            }, ct);

            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
