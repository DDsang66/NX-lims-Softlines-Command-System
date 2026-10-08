using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Application.Service;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[Controller]")]
    public class FiberAnalysisController : ControllerBase
    {
        private readonly FiberWorksheetService _worksheetService;
        private readonly IFiberReportFileStore _fiberReportStore;

        public FiberAnalysisController(
            FiberWorksheetService worksheetService,
            IFiberReportFileStore fiberReportStore)
        {
            _worksheetService = worksheetService;
            _fiberReportStore = fiberReportStore;
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

        /// <summary>
        /// 录入界面的分组候选：cellulosicSub / regeneratedSub / bicomponentSub / methodOptions。
        /// 原先这几组散在前端内联，且 cellulosic 那份是全小写、两个父槽共用一份。
        /// </summary>
        [HttpGet("fiber-options")]
        public async Task<IActionResult> GetFiberOptions()
            => Ok(await _worksheetService.GetFiberOptionsAsync());

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

        /// <summary>
        /// 下载报告文件(生成完当场下载走这条)。定位逻辑全在 store 里, 与历史列表共用同一把尺子。
        /// </summary>
        [HttpGet("{fileName}/download")]
        public IActionResult Download(string fileName)
        {
            string? filePath = _fiberReportStore.ResolvePath(fileName);
            if (filePath == null)
                return NotFound(new { success = false, message = "文件不存在" });

            return PhysicalFile(
                filePath,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileDownloadName: fileName,
                enableRangeProcessing: true
            );
        }

        /// <summary>
        /// 列历史报告文件(前端「历史报告」弹窗的数据源)。
        /// 扫 SaveDocx 根目录 + 全部 FiberAnalysis{yyyyMM} 子目录, 只认 *_FiberAnalysis.docx,
        /// 按生成时间倒序; keyword 非空时按报告号子串(不区分大小写)过滤。
        /// </summary>
        [HttpGet("reports")]
        public Result<List<FiberReportFileMeta>> ListReports([FromQuery] string? keyword)
            => Result<List<FiberReportFileMeta>>.Ok(_fiberReportStore.ListReports(keyword));

        /// <summary>
        /// 删除指定报告文件(历史列表"删除"按钮, 物理删除不可恢复, 前端已二次确认)。
        /// **只删文件, 不动库里 fiber_analysis 的行。**
        /// 文件名由 ListReports 提供; 不合法 / 不是纤维报告 / 文件已不在 → Fail。
        /// </summary>
        [HttpDelete("reports/{fileName}")]
        public Result<bool> DeleteReport(string fileName)
            => _fiberReportStore.DeleteReport(fileName)
                ? Result<bool>.Ok(true)
                : Result<bool>.Fail("报告文件不存在或不可删除");

        /// <summary>下载指定报告文件(历史列表, 文件名由 ListReports 提供)。与上面那条共用 ResolvePath。</summary>
        [HttpGet("reports/{fileName}")]
        public IActionResult DownloadReport(string fileName)
        {
            string? filePath = _fiberReportStore.ResolvePath(fileName);
            if (filePath == null)
                return NotFound(new { success = false, message = "报告文件不存在" });

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
