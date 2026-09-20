using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.DataSheetConetxt;
using NX_lims_Softlines_Command_System.src.Application.Service.DataSheetContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[Controller]")]
    public class DataSheetController : ControllerBase
    {
        private readonly DataSheetService _dataSheetService;
        private readonly DataSheetProgressService _progressService;
        private readonly DataSheetProgressHub _hub;
        public DataSheetController(
            DataSheetService dataSheetService,
            DataSheetProgressService progressService,
            DataSheetProgressHub hub) 
        {
            _dataSheetService = dataSheetService;
            _progressService = progressService;
            _hub = hub;
        }

        /// <summary>
        /// Start Generate DataSheet
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPost("generate")]
        public async Task<Result> GenerateDataSheet(DataSheetGenerateDto dto,CancellationToken ct) 
        {
            return await _dataSheetService.GenerateTaskStart1(dto,ct);
        }

        /// <summary>
        /// Start Generate DataSheet
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPost("datasheet-generate")]
        public async Task<Result<GenerateDataSheetTaskResult>> Generate([FromBody] DataSheetGenerateDto dto,CancellationToken ct)
        {
            var result = await _dataSheetService.GenerateTaskStart(dto, ct);

            return result;
        }

        /// <summary>
        /// 工作单生成进度
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("datasheet-progress")]
        public async Task<Result<DataSheetProgressDto>> GetProgress(Guid checkListId, CancellationToken ct)
        {
            var snapshot = await _progressService.GetSnapshot(
                new CheckListId(checkListId), ct);

            return Result<DataSheetProgressDto>.Ok(snapshot);
        }

        /// <summary>
        /// 工作单生成进度流
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpGet("datasheet-progress/stream")]
        public async Task StreamProgress(Guid checkListId, CancellationToken ct)
        {
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("X-Accel-Buffering", "no");

            var emitter = new SseEmitter(Response);
            _hub.Register(checkListId, emitter);

            try
            {
                var snapshot = await _progressService.GetSnapshot(
                    new CheckListId(checkListId), ct);
                await emitter.SendAsync("progress", snapshot);

                while (!ct.IsCancellationRequested)
                {
                    await Task.Delay(15000, ct);
                    await emitter.SendAsync("heartbeat", new { ts = DateTime.UtcNow });
                }
            }
            catch (OperationCanceledException) { }
            finally
            {
                _hub.Unregister(checkListId, emitter);
            }
        }

    }
}
