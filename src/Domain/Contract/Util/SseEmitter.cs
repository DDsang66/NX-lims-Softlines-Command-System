using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Util
{
    public class SseEmitter
    {
        private readonly HttpResponse _response;
        private readonly SemaphoreSlim _lock = new(1, 1);

        public SseEmitter(HttpResponse response) { _response = response; }

        public async Task SendAsync(string eventName, object payload)
        {
            await _lock.WaitAsync();
            try
            {
                var json = JsonSerializer.Serialize(payload);
                await _response.WriteAsync($"event: {eventName}\n");
                await _response.WriteAsync($"data: {json}\n\n");
                await _response.Body.FlushAsync();
            }
            finally { _lock.Release(); }
        }

        public void Complete() { /* 由请求管道负责结束 */ }
    }
}
