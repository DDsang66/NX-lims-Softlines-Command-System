using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Service;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;

namespace NX_lims_Softlines_Command_System.src.Web_API
{
    [ApiController]
    [Route("api/[controller]")]
    public class FiberController : ControllerBase
    {
        private readonly IFiberDatabaseRepository _fiberRepo;
        private readonly FiberWorksheetService _worksheetService;
        private readonly FiberCalculationService _calcService;

        public FiberController(
            IFiberDatabaseRepository fiberRepo,
            FiberWorksheetService worksheetService,
            FiberCalculationService calcService)
        {
            _fiberRepo = fiberRepo;
            _worksheetService = worksheetService;
            _calcService = calcService;
        }

        #region 纤维数据库 API

        /// <summary>
        /// 获取所有纤维数据
        /// </summary>
        [HttpGet("database")]
        public async Task<IActionResult> GetAllFibers()
        {
            var fibers = await _fiberRepo.GetAllAsync();
            var dtos = fibers.Select(f => new FiberDatabaseDto
            {
                Id = f.Id,
                FiberNameEn = f.FiberNameEn,
                FiberNameCn = f.FiberNameCn,
                Category = f.Category,
                MoistureRegainIso = f.MoistureRegainIso,
                MoistureRegainAatcc = f.MoistureRegainAatcc,
                MoistureRegainCan = f.MoistureRegainCan,
                MoistureRegainKor = f.MoistureRegainKor,
                MoistureRegainGb = f.MoistureRegainGb,
                MoistureRegainCns = f.MoistureRegainCns,
                MoistureRegainJis = f.MoistureRegainJis,
                QualitativeDescription = f.QualitativeDescription,
                IsActive = f.IsActive
            });

            return Ok(new { success = true, data = dtos });
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
            if (string.IsNullOrWhiteSpace(dto.FiberNameEn))
            {
                return BadRequest(new { success = false, message = "FiberNameEn is required" });
            }

            var existing = await _fiberRepo.GetByNameEnAsync(dto.FiberNameEn);
            if (existing != null)
            {
                return BadRequest(new { success = false, message = "Fiber already exists" });
            }

            var fiber = new FiberDatabase
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

            var result = await _fiberRepo.AddAsync(fiber);
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
            {
                return NotFound(new { success = false, message = "Fiber not found" });
            }

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
        /// 获取工作表
        /// </summary>
        [HttpGet("worksheet/{reportNumber}")]
        public async Task<IActionResult> GetWorksheet(string reportNumber)
        {
                var worksheet = await _worksheetService.GetByReportNumberAsync(reportNumber);
                if (worksheet == null)
                {
                    return NotFound(new { success = false, message = "Worksheet not found" });
                }

                return Ok(new { success = true, data = worksheet });
            }

        /// <summary>
        /// 保存工作表
        /// </summary>
        [HttpPost("worksheet")]
        public async Task<IActionResult> SaveWorksheet([FromBody] FiberWorksheetCreateDto dto)
        {
                if (string.IsNullOrWhiteSpace(dto.ReportNumber))
                {
                    return BadRequest(new { success = false, message = "ReportNumber is required" });
                }

                var result = await _worksheetService.SaveAsync(dto);
                return Ok(new { success = true, data = result });
            }

        /// <summary>
        /// 删除工作表
        /// </summary>
        [HttpDelete("worksheet/{id}")]
        public async Task<IActionResult> DeleteWorksheet(Guid id)
        {
                var result = await _worksheetService.DeleteAsync(id);
                return Ok(new { success = result });
            }

        #endregion

        #region 计算 API

        /// <summary>
        /// 计算纤维成分结果
        /// </summary>
        [HttpPost("calculate")]
        public async Task<IActionResult> Calculate([FromBody] FiberCalculationRequestDto request)
        {
                if (request.Items == null || request.Items.Count == 0)
                {
                    return BadRequest(new { success = false, message = "Items are required" });
                }

                var result = await _calcService.CalculateAsync(request);
                return Ok(new { success = true, data = result });
            }

        /// <summary>
        /// 计算并保存结果
        /// </summary>
        [HttpPost("calculate-and-save")]
        public async Task<IActionResult> CalculateAndSave([FromBody] FiberWorksheetCreateDto dto)
        {
                if (string.IsNullOrWhiteSpace(dto.ReportNumber))
                {
                    return BadRequest(new { success = false, message = "ReportNumber is required" });
                }

                var result = await _worksheetService.CalculateAndSaveAsync(dto);
                return Ok(new { success = true, data = result });
            }

        #endregion

        #region 文档 API

        /// <summary>
        /// 获取 Word 文档 URL（OnlyOffice 预览）
        /// </summary>
        [HttpGet("get-docxUrl")]
        public IActionResult GetDocxUrl()
        {
                // TODO: 根据实际需求返回文档 URL
                // 这里暂时返回一个占位符
                return Ok(new
                {
                    success = true,
                    url = "",  // 实际文档 URL
                    message = "Document generation not implemented yet"
                });
            }

        #endregion
    }
}
