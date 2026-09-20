using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.DataSheetConetxt;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using System.Collections.Concurrent;

namespace NX_lims_Softlines_Command_System.src.Application.Service.DataSheetContext
{
    public class DataSheetProgressHub:ISingletonDependency
    {
        private readonly ConcurrentDictionary<Guid, ConcurrentBag<SseEmitter>> _emitters = new();
        private readonly ConcurrentDictionary<Guid, string> _lastSnapshotHash = new();

        /// <summary>
        /// 注册监听
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="emitter"></param>
        public void Register(Guid checkListId, SseEmitter emitter)
        {
            var bag = _emitters.GetOrAdd(checkListId, _ => new ConcurrentBag<SseEmitter>());
            bag.Add(emitter);
        }

        /// <summary>
        /// 取消监听
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="emitter"></param>
        public void Unregister(Guid checkListId, SseEmitter emitter)
        {
            if (!_emitters.TryGetValue(checkListId, out var bag)) return;
            var newBag = new ConcurrentBag<SseEmitter>(bag.Where(e => !ReferenceEquals(e, emitter)));
            _emitters[checkListId] = newBag;
        }

        /// <summary>
        /// 获取所有激活的清单id
        /// </summary>
        /// <returns></returns>
        public IReadOnlyCollection<Guid> GetActiveCheckListIds()
            => _emitters.Keys.ToList();

        /// <summary>
        /// 判断是否发生变化
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="snapshot"></param>
        /// <returns></returns>
        public bool HasChanged(Guid checkListId, DataSheetProgressDto snapshot)
        {
            var hash = ComputeHash(snapshot);
            var last = _lastSnapshotHash.GetOrAdd(checkListId, _ => "");
            if (last == hash) return false;
            _lastSnapshotHash[checkListId] = hash;
            return true;
        }

        /// <summary>
        /// 广播
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="eventName"></param>
        /// <param name="payload"></param>
        /// <returns></returns>
        public async Task Broadcast(Guid checkListId, string eventName, object payload)
        {
            if (!_emitters.TryGetValue(checkListId, out var bag)) return;
            var dead = new List<SseEmitter>();
            foreach (var emitter in bag)
            {
                try { await emitter.SendAsync(eventName, payload); }
                catch { dead.Add(emitter); }
            }
            foreach (var d in dead) Unregister(checkListId, d);
        }

        /// <summary>
        /// 关闭所有监听
        /// </summary>
        /// <param name="checkListId"></param>
        public void CloseAll(Guid checkListId)
        {
            if (_emitters.TryRemove(checkListId, out var bag))
            {
                foreach (var e in bag)
                {
                    try { e.Complete(); } catch { }
                }
            }
            _lastSnapshotHash.TryRemove(checkListId, out _);
        }

        /// <summary>
        /// 计算hash
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        private static string ComputeHash(DataSheetProgressDto dto)
        {
            var raw = $"{dto.Status}|{dto.Success}|{dto.Failed}|{dto.Generating}|{dto.Pending}|{dto.MergedPdfUrl}|" +
                      string.Join(",", dto.Items.Select(i => $"{i.DataSheetId}:{i.Status}:{i.UpdatedAt.Ticks}"));

            var bytes = System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(raw));

            return Convert.ToHexString(bytes);
        }
    }
}
