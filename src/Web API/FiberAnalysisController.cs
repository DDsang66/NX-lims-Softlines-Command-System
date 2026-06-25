using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Domain.Model;
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
        private readonly IFiberWorksheetRepository _worksheetRepo;
        private readonly FiberWorksheetService _worksheetService;
        private readonly IWebHostEnvironment _env;
        private readonly LabDbContextSec _db;

        public FiberAnalysisController(
            IFiberDatabaseRepository fiberRepo,
            IFiberWorksheetRepository worksheetRepo,
            FiberWorksheetService worksheetService,
            IWebHostEnvironment env,
            LabDbContextSec db)
        {
            _fiberRepo = fiberRepo;
            _worksheetRepo = worksheetRepo;
            _worksheetService = worksheetService;
            _env = env;
            _db = db;
        }

        #region 纤维数据库 API

        [HttpGet("database")]
        public async Task<IActionResult> GetAllFibers()
        {
            var fibers = await _fiberRepo.GetAllAsync();
            return Ok(new { success = true, data = fibers });
        }

        [HttpGet("names")]
        public async Task<IActionResult> GetFiberNames()
        {
            var names = await _fiberRepo.GetAllNamesAsync();
            return Ok(new { success = true, data = names });
        }

        [HttpGet("label-options")]
        public async Task<IActionResult> GetLabelOptions(CancellationToken ct)
        {
            var options = await _db.LabelOptions
                .OrderBy(o => o.Category)
                .ThenBy(o => o.SortOrder)
                .Select(o => new { o.Category, o.Text })
                .ToListAsync(ct);

            var resultRemarkList = options
                .Where(o => o.Category == "ResultRemark")
                .Select(o => o.Text)
                .ToList();

            return Ok(new
            {
                success = true,
                data = new
                {
                    judgmentLabelOptions = options
                        .Where(o => o.Category == "Judgment")
                        .Select(o => o.Text)
                        .ToList(),
                    languageLabelOptions = options
                        .Where(o => o.Category == "Language")
                        .Select(o => o.Text)
                        .ToList(),
                    resultRemarkOptions = resultRemarkList,
                    labelRemarkOptions = resultRemarkList
                }
            });
        }

        [HttpPost("database")]
        public async Task<IActionResult> AddFiber([FromBody] FiberDatabaseCreateDto dto)
        {
            var entity = new CompositionNew
            {
                CompositionNameEn = dto.FiberNameEn,
                CompositionNameChn = dto.FiberNameCn,
                PrimaryCategoryEn = dto.Category
            };
            var result = await _fiberRepo.AddAsync(entity);
            return Ok(new { success = true, data = result });
        }

        [HttpPut("database/{id}")]
        public async Task<IActionResult> UpdateFiber(Guid id, [FromBody] FiberDatabaseCreateDto dto)
        {
            var fiber = await _fiberRepo.GetByIdAsync(id);
            if (fiber == null)
                return NotFound(new { success = false, message = "纤维数据不存在" });

            fiber.CompositionNameEn = dto.FiberNameEn;
            fiber.CompositionNameChn = dto.FiberNameCn;
            fiber.PrimaryCategoryEn = dto.Category;

            var result = await _fiberRepo.UpdateAsync(fiber);
            return Ok(new { success = true, data = result });
        }

        [HttpDelete("database/{id}")]
        public async Task<IActionResult> DeleteFiber(Guid id)
        {
            var result = await _fiberRepo.DeleteAsync(id);
            return Ok(new { success = result });
        }

        #endregion

        #region 工作表 API

        [HttpPost("worksheet")]
        public async Task<Result<DocxUrlResponseDto>> BuildAnalysis([FromBody] BuildAnalysisDto dto, CancellationToken ct)
        {
            var result = await _worksheetService.BuildAnalysisAsync(dto, ct);
            if (result.IsFailure)
                return Result<DocxUrlResponseDto>.Fail(result.Error, result.ErrorCode);

            var actualFileName = result.Value;  // Service 返回的实际文件名（含时间戳）
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
            var result = await _worksheetRepo.GetByReportNumberAsync(reportNumber);
            if (result == null)
                return NotFound(new { success = false, message = "工作表不存在" });

            return Ok(new { success = true, data = result });
        }

        [HttpDelete("worksheet/{id}")]
        public async Task<IActionResult> DeleteWorksheet(Guid id)
        {
            var result = await _worksheetRepo.DeleteAsync(id);
            if (!result)
                return NotFound(new { success = false, message = "工作表不存在或删除失败" });

            return Ok(new { success = true });
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
