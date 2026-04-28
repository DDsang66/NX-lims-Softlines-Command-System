using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Service;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.FiberContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[Controller]")]
    public class FiberAnalysisController : ControllerBase
    {
        private readonly IFiberDatabaseRepository _fiberRepo;
        private readonly FiberWorksheetService _worksheetService;

        public FiberAnalysisController(
            IFiberDatabaseRepository fiberRepo,
            FiberWorksheetService worksheetService)
        {
            _fiberRepo = fiberRepo;
            _worksheetService = worksheetService;
        }

        #region 纤维数据库 API

        /// <summary>
        /// 获取所有纤维数据
        /// </summary>
        [HttpGet("database")]
        public async Task<IActionResult> GetAllFibers()
        {
            var fibers = await _fiberRepo.GetAllAsync();

            return Ok(new { success = true });
        }

        /// <summary>
        /// 获取纤维名称列表（用于前端下拉选择）
        /// </summary>
        [HttpGet("names")]
        public async Task<IActionResult> GetFiberNames()
        {
            var names = await _fiberRepo.GetAllNamesAsync();

            return Ok(new { success = true, data = names });
        }

        /// <summary>
        /// 添加纤维数据
        /// </summary>
        [HttpPost("database")]
        public async Task<IActionResult> AddFiber([FromBody] FiberDatabaseCreateDto dto)
        {

            return Ok(new { success = true });
        }

        /// <summary>
        /// 更新纤维数据
        /// </summary>
        [HttpPut("database/{id}")]
        public async Task<IActionResult> UpdateFiber(Guid id, [FromBody] FiberDatabaseCreateDto dto)
        {
            var fiber = await _fiberRepo.GetByIdAsync(id);

            return Ok(new { success = true });
        }

        /// <summary>
        /// 删除纤维数据
        /// </summary>
        [HttpDelete("database/{id}")]
        public async Task<IActionResult> DeleteFiber(Guid id)
        {
            var result = await _fiberRepo.DeleteAsync(id);

            return Ok(new { success = result });
        }

        #endregion

        #region 工作表 API
        /// <summary>
        /// 返回下载地址url
        /// 前端在传给document server进行渲染
        /// </summary>
        [HttpPost("worksheet")]
        public async Task<Result<DocxUrlResponseDto>> BuildAnalysis([FromBody] BuildAnalysisDto dto,CancellationToken ct)
        {
            var result = await _worksheetService.BuildAnalysisAsync(dto,ct);

            return Result<DocxUrlResponseDto>.Ok(new DocxUrlResponseDto());
        }

        /// <summary>
        /// 工作表下载地址
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        [HttpGet("{fileName}/download")]
        public IActionResult Download(string fileName)
        {
            // 根据 fileId 找到实际文件路径

            string filePath = null;
            // 返回文件流，Content-Type 必须正确
            return PhysicalFile(
                filePath,
              "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileDownloadName: fileName,
                enableRangeProcessing: true  // 支持断点续传
            );
        }


        /// <summary>
        /// 获取工作表
        /// </summary>
        [HttpGet("worksheet/{reportNumber}")]
        public async Task<IActionResult> GetWorkSheet()
        {
            //var result = await _worksheetService.SaveAsync(dto);

            return Ok(new { success = true});
        }

        /// <summary>
        /// 删除工作表
        /// </summary>
        [HttpDelete("worksheet/{id}")]
        public async Task<IActionResult> DeleteWorksheet(Guid id)
        {
            //var result = await _worksheetService.DeleteAsync(id);
            return Ok(new { });
        }

        #endregion

        #region 计算 API

        /// <summary>
        /// 执行成分逻辑计算获取Remark、Label等数据
        /// </summary>
        [HttpPost("calculate")]
        public async Task<Result> Calculate([FromBody] FiberCalculationRequestDto request)
        {
            //var result = await _calcService.CalculateAsync(request);

            return Result.Ok();
        }
        #endregion
    }
}
