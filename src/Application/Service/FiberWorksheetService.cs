using DocumentFormat.OpenXml.Office2010.Excel;
using Mapster;
using NX_lims_Softlines_Command_System.Application.Services.AuthenticationService;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.IngredientAnalysis;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.IngredientAnalysis.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.IngredientAnalysis.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Application.Service
{
    /// <summary>
    /// 纤维工作表服务
    /// </summary>
    public class FiberWorksheetService : IScopedDependency
    {
        private readonly IFiberWorksheetRepository _worksheetRepo;

        public FiberWorksheetService(IFiberWorksheetRepository worksheetRepo)
        {
            _worksheetRepo = worksheetRepo;
        }

        /// <summary>
        /// 构建成分分析报告服务
        /// </summary>
        /// <returns></returns>0
        public async Task<Result> BuildAnalysisAsync(BuildAnalysisDto dto,CancellationToken ct) 
        {
            await ValidateStructure(dto);

            var entity = dto.Adapt<FiberAnalysis>();

            var snowflake = new SnowflakeIdGenerator();

            entity.Id = snowflake.NextId();

            await _worksheetRepo.AddAsync(entity, ct);

            var po = await _worksheetRepo.GetByIdAsync(entity.Id, ct);

            if (po == null) return Result.Fail("data is not found");

            var ingredientsAnalysis = po.Adapt<IngredientAnalysis>();//Mapster封装映射，内部使用工厂模式构建IngredientAnalysis对象

            //执行计算
            try
            {
                ingredientsAnalysis.Calculator();
            }
            catch (Exception ex)
            {
                //记录日志
                //触发补偿机制
            }

            //计算失败触发补偿机制

            //执行生成word
            string filePath = string.Empty;//先调用生成方法成功后获取filePath

            ingredientsAnalysis.WorkSheetGenerator(filePath);
            //执行保存

            //IFiberReposity 查询Word地址与生成状态；返回url

            return Result.Ok();
        }




        /// <summary>
        /// 轻量结构性验证
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
