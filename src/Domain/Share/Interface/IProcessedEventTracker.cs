using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Share.Interface
{
    public interface IProcessedEventTracker: IScopedDependency
    {
        /// <summary>
        /// 判断事件是否已处理
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<bool> IsProcessedAsync(Guid eventId, CancellationToken ct);

        /// <summary>
        /// 标记事件为已处理
        /// </summary>
        /// <param name="eventId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task MarkAsProcessedAsync(Guid eventId, CancellationToken ct);
    }
}
