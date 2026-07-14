using NX_lims_Softlines_Command_System.src.Domain.Events;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Domain.Share.Interface
{
    public interface IEventOutbox: IScopedDependency
    {
        /// <summary>
        /// 将事件存储进入数据库，不保存
        /// </summary>
        /// <param name="event"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task StoreAsync(DomainEvent @event, CancellationToken ct);

        /// <summary>
        /// 获取未发布的事件
        /// </summary>
        /// <param name="batchSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<DomainEvent>> GetUnpublishedEventsAsync(int batchSize, CancellationToken ct);
        
        /// <summary>
        /// 标记事件为已发布
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task MarkAsPublishedAsync(Guid eventId, CancellationToken ct);

        /// <summary>
        /// 获取未发布的事件数量
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="error"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task IncrementRetryAsync(Guid eventId, string error, CancellationToken ct); 

        /// <summary>
        /// 标记事件为死信
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task MarkAsDeadLetterAsync(Guid eventId, CancellationToken ct);  

        /// <summary>
        /// 查询所有出箱的事件
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<OutboxEntry?> GetEntryAsync(Guid eventId, CancellationToken ct);  
    }
}
