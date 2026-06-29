using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Service;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[Controller]")]
    public class FiberAnalysisController : ControllerBase
    {
        private readonly FiberWorksheetService _worksheetService;
        private readonly IWebHostEnvironment _env;

        public FiberAnalysisController(
            FiberWorksheetService worksheetService,
            IWebHostEnvironment env)
        {
            _worksheetService = worksheetService;
            _env = env;
        }

        #region 纤维数据库 API

        [HttpGet("database")]
        public async Task<IActionResult> GetAllFibers()
            => Ok(await _worksheetService.GetAllFibersAsync());

        [HttpGet("names")]
        public async Task<IActionResult> GetFiberNames()
            => Ok(await _worksheetService.GetFiberNamesAsync());

        [HttpGet("label-options")]
        public async Task<IActionResult> GetLabelOptions(CancellationToken ct)
            => Ok(await _worksheetService.GetLabelOptionsAsync(ct));

        [HttpPost("database")]
        public async Task<IActionResult> AddFiber([FromBody] FiberDatabaseCreateDto dto)
            => Ok(await _worksheetService.AddFiberAsync(dto));

        [HttpPut("database/{id}")]
        public async Task<IActionResult> UpdateFiber(Guid id, [FromBody] FiberDatabaseCreateDto dto)
        {
            var result = await _worksheetService.UpdateFiberAsync(id, dto);
            var obj = result as dynamic;
            if (obj?.success == false) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("database/{id}")]
        public async Task<IActionResult> DeleteFiber(Guid id)
            => Ok(await _worksheetService.DeleteFiberAsync(id));

        #endregion

        #region 工作表 API

        [HttpPost("worksheet")]
        public async Task<Result<DocxUrlResponseDto>> BuildAnalysis([FromBody] BuildAnalysisDto dto, CancellationToken ct)
        {
            var result = await _worksheetService.BuildAnalysisAsync(dto, ct);
            if (result.IsFailure)
                return Result<DocxUrlResponseDto>.Fail(result.Error, result.ErrorCode);

            var actualFileName = result.Value;
            var docxUrl = new DocxUrlResponseDto
            {
                fileKey = actualFileName,
                fileName = actualFileName,
                downloadUrl = $"/api/FiberAnalysis/{actualFileName}/download",
                callbackUrl = $"/api/FiberAnalysis/{actualFileName}/callback"
            };

            return Result<DocxUrlResponseDto>.Ok(docxUrl);
        }

        [HttpGet("{fileName}/download")]
        public IActionResult Download(string fileName)
        {
            var filePath = Path.Combine(_env.WebRootPath, "DocxModel", "SaveDocx", fileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound(new { success = false, message = "文件不存在" });

            return PhysicalFile(
                filePath,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileDownloadName: fileName,
                enableRangeProcessing: true
            );
        }

        [HttpGet("worksheet/{reportNumber:regex(^.+$)}")]
        public async Task<IActionResult> GetWorkSheet(string reportNumber)
        {
            var result = await _worksheetService.GetWorkSheetAsync(reportNumber);
            var obj = result as dynamic;
            if (obj?.success == false) return NotFound(result);
            return Ok(result);
        }

        [HttpDelete("worksheet/{id}")]
        public async Task<IActionResult> DeleteWorksheet(Guid id)
        {
            var result = await _worksheetService.DeleteWorksheetAsync(id);
            var obj = result as dynamic;
            if (obj?.success == false) return NotFound(result);
            return Ok(result);
        }

        #endregion

        #region 计算 API

        [HttpPost("calculate")]
        public async Task<Result<FiberCalculationResultDto>> Calculate([FromBody] FiberCalculationRequestDto request)
        {
            // 纯计算，不持久化
            var result = await _worksheetService.DirectCalculateAsync(request);
            if (result.IsFailure)
                return Result<FiberCalculationResultDto>.Fail(result.Error, result.ErrorCode);
            return result;
        }

        [HttpPost("calculate/report/{reportNumber:regex(^.+$)}")]
        public async Task<Result<FiberCalculationResultDto>> CalculateByReport(string reportNumber)
        {
            var result = await _worksheetService.CalculateByReportAsync(reportNumber);
            if (result.IsFailure)
                return Result<FiberCalculationResultDto>.Fail(result.Error, result.ErrorCode);
            return result;
        }

        [HttpPost("calculate/{id:long}")]
        public async Task<Result<string>> CalculateById(long id, CancellationToken ct)
        {
            var result = await _worksheetService.CalculateAsync(id, ct);
            if (result.IsFailure)
                return Result<string>.Fail(result.Error, result.ErrorCode);
            return Result<string>.Ok("计算完成");
        }

        #endregion
    }
}
