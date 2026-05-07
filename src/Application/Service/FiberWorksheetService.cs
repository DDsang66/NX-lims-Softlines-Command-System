using DocumentFormat.OpenXml.Office2010.Excel;
using Mapster;
using NX_lims_Softlines_Command_System.Application.Services.AuthenticationService;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.AnalysisWorksheet;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.FiberContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine.WordTemplateAdapter;
using System.ComponentModel.DataAnnotations;
using System.Reflection.Metadata;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Application.Service
{
    /// <summary>
    /// 纤维工作表服务
    /// </summary>
    public class FiberWorksheetService : IScopedDependency
    {
        private readonly IFiberWorksheetRepository _worksheetRepo;
        private readonly FiberAnalysisWordTemplateAdapter _wordTemplateAdapter;

        public FiberWorksheetService(IFiberWorksheetRepository worksheetRepo, FiberAnalysisWordTemplateAdapter wordTemplateAdapter)
        {
            _worksheetRepo = worksheetRepo;
            _wordTemplateAdapter = wordTemplateAdapter;
        }

        /// <summary>
        /// 构建成分分析报告服务
        /// </summary>
        /// <returns></returns>0
        public async Task<Result> BuildAnalysisAsync(BuildAnalysisDto dto,CancellationToken ct) 
        {

            //需要重新进行用例编排
            //实例化AnalysisWorksheet生成word文件
            //实例化IngredientAnalysis进行计算
            //结果回传AnalysisWorksheet进行文档填写

            await ValidateStructure(dto);

            var entity = dto.Adapt<FiberAnalysis>();

            var snowflake = new SnowflakeIdGenerator();

            entity.Id = snowflake.NextId();

            await _worksheetRepo.AddAsync(entity, ct);

            var po = await _worksheetRepo.GetByIdAsync(entity.Id, ct);

            if (po == null) return Result.Fail("data is not found");

            var ingredientsAnalysis = po.Adapt<IngredientAnalysisCalculation>();//Mapster封装映射，内部使用工厂模式构建IngredientAnalysis对象

            //执行计算
            try
            {
                await ingredientsAnalysis.CalculateAsync();
                //计算失败触发补偿机制

                //执行生成word
                string filePath = string.Empty;//先调用生成方法成功后获取filePath

                var analysisWorksheet = AnalysisWorksheet.Create();

                analysisWorksheet.AttachCalculationResult(ingredientsAnalysis.Result);

                var templateData = _wordTemplateAdapter.Adapt(analysisWorksheet.CalculationResult);
                //ingredientsAnalysis.WorkSheetGenerator(filePath);
                //执行保存

                //IFiberReposity 查询Word地址与生成状态；返回url

                return Result.Ok();
            }
            catch (Exception ex)
            {
                //记录日志
                //触发补偿机制
                return Result.Fail(ex.Message);
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
        /// 计算成分分析结果
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<AnalysisResult>> CalculateAsync(long id, CancellationToken ct)
        {
            return Result<AnalysisResult>.Ok(new AnalysisResult());
        }

        /// <summary>
        /// 生成 Word（依赖计算）
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<string>> GenerateWordAsync(long id, CancellationToken ct)
        {
            string filePath = string.Empty;
            return Result<string>.Ok(filePath);
        }

        /// <summary>
        /// 结构性验证
        /// </summary>
        /// <returns></returns>
        public async Task<Result> ValidateStructure(BuildAnalysisDto dto)
        {
            if (string.IsNullOrEmpty(dto.ReportNumber))
                throw new ValidationException("报告号必填");

            if (!dto.Method.Any())
                throw new ValidationException("检测方法必填");

            var hasSingle = dto.SingleBuildAnalysis?.SingleFiberRows?.Any() == true;

            var hasMultiple = dto.MultipleBuildAnalysis?.fiberSplittingList?.Any() == true
                || dto.MultipleBuildAnalysis?.fiberDissolvedList?.Any() == true;

            if (!hasSingle && !hasMultiple)
                throw new ValidationException("至少包含一组分数据");

            return Result.Ok();
        }
    }
}
