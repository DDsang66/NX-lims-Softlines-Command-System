using Mapster;
using NX_lims_Softlines_Command_System.Application.Services.AuthenticationService;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.FiberContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Application.Contract;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Application.Service
{
    /// <summary>
    /// 纤维工作表服务
    /// </summary>
    public class FiberWorksheetService : IScopedDependency
    {
        private readonly IFiberWorksheetRepository _worksheetRepo;
        private readonly IFiberDatabaseRepository _fiberDatabaseRepo;
        private readonly IWordTemplateAdapter _wordTemplateAdapter;
        private readonly IWordTemplateEngine _wordTemplateEngine;
        private readonly IFileStorageService _fileStorage;
        private readonly ILabelOptionRepository _labelOptionRepo;

        public FiberWorksheetService(
            IFiberWorksheetRepository worksheetRepo,
            IFiberDatabaseRepository fiberDatabaseRepo,
            IWordTemplateAdapter wordTemplateAdapter,
            IWordTemplateEngine wordTemplateEngine,
            IFileStorageService fileStorage,
            ILabelOptionRepository labelOptionRepo)
        {
            _worksheetRepo = worksheetRepo;
            _fiberDatabaseRepo = fiberDatabaseRepo;
            _wordTemplateAdapter = wordTemplateAdapter;
            _wordTemplateEngine = wordTemplateEngine;
            _fileStorage = fileStorage;
            _labelOptionRepo = labelOptionRepo;
        }

        public async Task<object> GetLabelOptionsAsync(CancellationToken ct)
        {
            var options = await _labelOptionRepo.GetLabelOptionsAsync(ct);
            var resultRemarkList = options.Where(o => o.Category == "ResultRemark").Select(o => o.Text).ToList();
            return new
            {
                success = true,
                data = new
                {
                    judgmentLabelOptions = options.Where(o => o.Category == "Judgment").Select(o => o.Text).ToList(),
                    languageLabelOptions = options.Where(o => o.Category == "Language").Select(o => o.Text).ToList(),
                    resultRemarkOptions = resultRemarkList,
                    labelRemarkOptions = resultRemarkList
                }
            };
        }

        public async Task<object> GetAllFibersAsync()
            => new { success = true, data = await _fiberDatabaseRepo.GetAllAsync() };

        public async Task<object> GetFiberNamesAsync()
            => new { success = true, data = await _fiberDatabaseRepo.GetAllNamesAsync() };

        public async Task<object> AddFiberAsync(FiberDatabaseCreateDto dto)
        {
            var entity = new CompositionNew
            {
                CompositionNameEn = dto.FiberNameEn,
                CompositionNameChn = dto.FiberNameCn,
                PrimaryCategoryEn = dto.Category
            };
            var result = await _fiberDatabaseRepo.AddAsync(entity);
            return new { success = true, data = result };
        }

        public async Task<object> UpdateFiberAsync(Guid id, FiberDatabaseCreateDto dto)
        {
            var fiber = await _fiberDatabaseRepo.GetByIdAsync(id);
            if (fiber == null) return new { success = false, message = "纤维数据不存在" };
            fiber.CompositionNameEn = dto.FiberNameEn;
            fiber.CompositionNameChn = dto.FiberNameCn;
            fiber.PrimaryCategoryEn = dto.Category;
            var result = await _fiberDatabaseRepo.UpdateAsync(fiber);
            return new { success = true, data = result };
        }

        public async Task<object> DeleteFiberAsync(Guid id)
        {
            var result = await _fiberDatabaseRepo.DeleteAsync(id);
            return new { success = result };
        }

        public async Task<object> GetWorkSheetAsync(string reportNumber)
        {
            var result = await _worksheetRepo.GetByReportNumberAsync(reportNumber);
            if (result == null) return new { success = false, message = "工作表不存在" };
            return new { success = true, data = result };
        }

        public async Task<object> DeleteWorksheetAsync(Guid id)
        {
            var result = await _worksheetRepo.DeleteAsync(id);
            return new { success = result };
        }

        /// <summary>
        /// 构建成分分析报告服务
        /// </summary>
        /// <returns></returns>0
        public async Task<Result<string>> BuildAnalysisAsync(BuildAnalysisDto dto,CancellationToken ct)
        {

            //需要重新进行用例编排
            //实例化AnalysisWorksheet生成word文件
            //实例化IngredientAnalysis进行计算
            //结果回传AnalysisWorksheet进行文档填写

            var entity = dto.Adapt<FiberAnalysis>();

            var snowflake = new SnowflakeIdGenerator();

            entity.Id = snowflake.NextId();

            await _worksheetRepo.AddAsync(entity, ct);

            var po = await _worksheetRepo.GetByIdAsync(entity.Id, ct);

            if (po == null) return Result<string>.Fail("data is not found");

            var ingredientsAnalysis = po.Adapt<IngredientAnalysisCalculation>();//Mapster封装映射，内部使用工厂模式构建IngredientAnalysis对象

            // 回潮率查询
            var selectedStandard2 = ingredientsAnalysis.Methods.FirstOrDefault() ?? string.Empty;
            var mrMap = await _fiberDatabaseRepo.GetMoistureRegainMapAsync(selectedStandard2);

            //执行计算
            try
            {
                ingredientsAnalysis.Calculate(mrMap);
                //计算失败触发补偿机制

                //执行生成word
                string targetFileName = $"{ingredientsAnalysis.ReportNo}_{DateTime.Now:yyMMddHHmmss}_FiberAnalysis.docx";
                string targetPath = _fileStorage.CopyTemplate(
                    Path.Combine("DocxModel", "FIBER_ANALYSIS_DATA_SHEET.docx"),
                    Path.Combine("DocxModel", "SaveDocx"),
                    targetFileName);

                var templateData = _wordTemplateAdapter.Adapt(ingredientsAnalysis.Result);

                _wordTemplateEngine.ReplaceText(targetPath, templateData);

                // 显微镜图片插入 — 展开多组分所有纤维名
                var fiberNames = ingredientsAnalysis.Components
                    .SelectMany(c => c is DissolvedFiberComponent d
                        ? d.DissolutionUnits.Select(u => u.FiberName)
                        : new[] { c.FiberName })
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var imageFolder = Path.Combine("wwwroot", "MicroscopeImages");
                _wordTemplateEngine.InsertMicroscopeImages(targetPath, fiberNames, imageFolder);

                //ingredientsAnalysis.WorkSheetGenerator(filePath);
                //执行保存

                //IFiberReposity 查询Word地址与生成状态；返回url

                return Result<string>.Ok(targetFileName);
            }
            catch (Exception ex)
            {
                //记录日志
                //触发补偿机制
                return Result<string>.Fail(ex.Message);
            }
        }


        /// <summary>
        /// 计算用户的输入值（单项分析）并返回分析结果（不保存数据，仅供前端展示）
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<string>> InputValueCalculateAsync(long id, CancellationToken ct)
        {
            return Result<string>.Ok(string.Empty);
        }

        /// <summary>
        /// 创建分析记录（只保存原始数据）
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<long>> CreateAsync(BuildAnalysisDto dto, CancellationToken ct)
        {
            long id = 1;
            return Result<long>.Ok(id);
        }

        /// <summary>
        /// 按 ID 计算成分分析结果
        /// </summary>
        public async Task<Result<AnalysisResult>> CalculateAsync(long id, CancellationToken ct)
        {
            var entity = await _worksheetRepo.GetByIdAsync(id, ct);
            if (entity == null)
                return Result<AnalysisResult>.Fail("分析记录不存在", "NOT_FOUND");

            var ingredientsAnalysis = entity.Adapt<IngredientAnalysisCalculation>();
            ingredientsAnalysis.Calculate();

            return Result<AnalysisResult>.Ok(ingredientsAnalysis.Result);
        }

        /// <summary>
        /// 按报告号计算（先查 ID 再计算）
        /// </summary>
        public async Task<Result<FiberCalculationResultDto>> CalculateByReportAsync(string reportNumber)
        {
            var entity = await _worksheetRepo.GetByReportNumberAsync(reportNumber);
            if (entity == null)
                return Result<FiberCalculationResultDto>.Fail("工作表不存在", "NOT_FOUND");

            var ingredientsAnalysis = entity.Adapt<IngredientAnalysisCalculation>();
            var calcResult = ingredientsAnalysis.Calculate();

            return Result<FiberCalculationResultDto>.Ok(new FiberCalculationResultDto
            {
                RecommendedLabel = calcResult.RecommendedLabelString,
                Items = calcResult.Recommendation?
                    .Select(r => new FiberCalculationItemResultDto { Composition = r })
                    .ToList() ?? new()
            });
        }

        /// <summary>
        /// 纯计算（不持久化）
        /// </summary>
        public async Task<Result<FiberCalculationResultDto>> DirectCalculateAsync(FiberCalculationRequestDto request)
        {
            return Result<FiberCalculationResultDto>.Fail("请先保存工作单后执行计算", "NOT_IMPLEMENTED");
        }

        /// <summary>
        /// 生成 Word（依赖计算）
        /// </summary>
        public async Task<Result<string>> GenerateWordAsync(long id, CancellationToken ct)
        {
            var entity = await _worksheetRepo.GetByIdAsync(id, ct);
            if (entity == null)
                return Result<string>.Fail("分析记录不存在", "NOT_FOUND");

            var targetPath = Path.Combine("wwwroot", "DocxModel", "SaveDocx", $"{entity.ReportNumber}_FiberAnalysis.docx");
            return Result<string>.Ok(targetPath);
        }

        /// <summary>
    }
}
