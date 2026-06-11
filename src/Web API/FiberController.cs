using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Service;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[Controller]")]
    public class FiberController : ControllerBase
    {
        private readonly IFiberDatabaseRepository _fiberRepo;
        private readonly IFiberWorksheetRepository _worksheetRepo;
        private readonly FiberWorksheetService _worksheetService;
        private readonly FiberCalculationService _calcService;
        private readonly IWebHostEnvironment _env;

        public FiberController(
            IFiberDatabaseRepository fiberRepo,
            IFiberWorksheetRepository worksheetRepo,
            FiberWorksheetService worksheetService,
            FiberCalculationService calcService,
            IWebHostEnvironment env)
        {
            _fiberRepo = fiberRepo;
            _worksheetRepo = worksheetRepo;
            _worksheetService = worksheetService;
            _calcService = calcService;
            _env = env;
        }

        #region 纤维数据库 API

        /// <summary>
        /// 获取所有纤维数据
        /// </summary>
        [HttpGet("database")]
        public async Task<IActionResult> GetAllFibers()
        {
            var fibers = await _fiberRepo.GetAllAsync();

            return Ok(new { success = true, data = fibers });
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
            var entity = new FiberDatabase
            {
                FiberNameEn = dto.FiberNameEn,
                FiberNameCn = dto.FiberNameCn,
                Category = dto.Category,
                MoistureRegainIso = dto.MoistureRegainIso,
                MoistureRegainAatcc = dto.MoistureRegainAatcc,
                MoistureRegainCan = dto.MoistureRegainCan,
                MoistureRegainKor = dto.MoistureRegainKor,
                MoistureRegainGb = dto.MoistureRegainGb,
                MoistureRegainCns = dto.MoistureRegainCns,
                MoistureRegainJis = dto.MoistureRegainJis,
                QualitativeDescription = dto.QualitativeDescription
            };

            var result = await _fiberRepo.AddAsync(entity);

            return Ok(new { success = true, data = result });
        }

        /// <summary>
        /// 更新纤维数据
        /// </summary>
        [HttpPut("database/{id}")]
        public async Task<IActionResult> UpdateFiber(Guid id, [FromBody] FiberDatabaseCreateDto dto)
        {
            var fiber = await _fiberRepo.GetByIdAsync(id);
            if (fiber == null)
                return NotFound(new { success = false, message = "纤维数据不存在" });

            fiber.FiberNameEn = dto.FiberNameEn;
            fiber.FiberNameCn = dto.FiberNameCn;
            fiber.Category = dto.Category;
            fiber.MoistureRegainIso = dto.MoistureRegainIso;
            fiber.MoistureRegainAatcc = dto.MoistureRegainAatcc;
            fiber.MoistureRegainCan = dto.MoistureRegainCan;
            fiber.MoistureRegainKor = dto.MoistureRegainKor;
            fiber.MoistureRegainGb = dto.MoistureRegainGb;
            fiber.MoistureRegainCns = dto.MoistureRegainCns;
            fiber.MoistureRegainJis = dto.MoistureRegainJis;
            fiber.QualitativeDescription = dto.QualitativeDescription;

            var result = await _fiberRepo.UpdateAsync(fiber);

            return Ok(new { success = true, data = result });
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
        public async Task<Result<DocxUrlResponseDto>> BuildAnalysis([FromBody] BuildAnalysisDto dto)
        {
            try
            {
            var result = await _worksheetService.BuildAnalysisAsync(dto);
            if (result.IsFailure)
                return Result<DocxUrlResponseDto>.Fail(result.Error, result.ErrorCode);

            var docxUrl = new DocxUrlResponseDto
            {
                fileKey = dto.ReportNumber,
                fileName = $"FIBER_ANALYSIS_{dto.ReportNumber}.docx",
                downloadUrl = $"/api/fiber/{dto.ReportNumber}/download",
                callbackUrl = $"/api/fiber/{dto.ReportNumber}/callback"
            };

            return Result<DocxUrlResponseDto>.Ok(docxUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WS_500] {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[WS_500_INNER] {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
                throw;
            }
        }

        /// <summary>
        /// 工作表下载地址
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        [HttpGet("{fileName}/download")]
        public IActionResult Download(string fileName)
        {
            var filePath = Path.Combine(_env.WebRootPath, "DocxModel", fileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound(new { success = false, message = "文件不存在" });

            return PhysicalFile(
                filePath,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                fileDownloadName: fileName,
                enableRangeProcessing: true
            );
        }


        /// <summary>
        /// 获取工作表
        /// </summary>
        [HttpGet("worksheet/{reportNumber}")]
        public async Task<IActionResult> GetWorkSheet(string reportNumber)
        {
            var result = await _worksheetService.GetWorksheetAsync(reportNumber);
            if (result == null)
                return NotFound(new { success = false, message = "工作表不存在" });

            return Ok(new { success = true, data = result });
        }

        /// <summary>
        /// 删除工作表
        /// </summary>
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

        /// <summary>
        /// 执行成分逻辑计算获取Remark、Label等数据
        /// </summary>
        [HttpPost("calculate")]
        public async Task<Result<FiberCalculationResultDto>> Calculate([FromBody] FiberCalculationRequestDto request)
        {
            var result = await _calcService.CalculateAsync(request);

            return Result<FiberCalculationResultDto>.Ok(result);
        }

        /// <summary>
        /// 根据工作表数据执行计算并更新Remark/Label
        /// </summary>
        [HttpPost("calculate/{reportNumber}")]
        public async Task<Result<FiberCalculationResultDto>> CalculateByReport(string reportNumber, [FromQuery] string standard = "ISO")
        {
            var result = await _worksheetService.CalculateRemarkAsync(reportNumber, standard);
            if (result.IsFailure)
                return Result<FiberCalculationResultDto>.Fail(result.Error, result.ErrorCode);

            return result;
        }
        #endregion
    }
}
