using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Services.Validate
{
    public class ParamStructureValidateService: IScopedDependency
    {
        private readonly IParamStructureRepository _paramStructureRepository;
        // 假设还需要校验关联的 Formula 或 StandardFamily 聚合根
        // private readonly IFormulaRepository _formulaRepository;
        // private readonly IStandardFamilyRepository _standardFamilyRepository;

        public ParamStructureValidateService(
            IParamStructureRepository paramStructureRepository
            /*, IFormulaRepository formulaRepository, IStandardFamilyRepository standardFamilyRepository */)
        {
            _paramStructureRepository = paramStructureRepository;
            // _formulaRepository = formulaRepository;
            // _standardFamilyRepository = standardFamilyRepository;
        }


        /// <summary>
        /// 协调校验：在创建或更新 ParamStructure 时，验证其跨聚合根的合法性
        /// </summary>
        /// <param name="paramStructure">待校验的聚合根实例</param>
        /// <param name="isUpdate">是否为更新操作（更新时排除自身ID的重复校验）</param>
        public async Task ValidateAsync(ParamStructure paramStructure, bool isUpdate = false)
        {
            if (paramStructure == null)
                throw new ArgumentNullException(nameof(paramStructure));

            // 1. 校验当前 ParamStructure 是否重复（例如：同一标准族下参数名唯一）
            await ValidateUniquenessAsync(paramStructure, isUpdate);

            // 2. 校验关联的其他聚合根是否存在且合法
            await ValidateRelatedAggregatesAsync(paramStructure);
        }

        /// <summary>
        /// 校验唯一性：例如同一 StandardFamily 下不能有重名的 ParamStructure
        /// </summary>
        private async Task ValidateUniquenessAsync(ParamStructure paramStructure, bool isUpdate)
        {
            // 假设业务规则：在同一个标准族下，参数名不能重复
            // 根据你的实际业务规则调整查询条件
            //var existingParam = await _paramStructureRepository.FindByStandardFamilyAndNameAsync(
            //    paramStructure.StandardFamilyIds.FirstOrDefault(), // 简化示例，取第一个标准族ID
            //    paramStructure.ParamName
            //);

            //if (existingParam != null)
            //{
            //    // 如果是更新操作，查到的可能是自己，此时不算重复
            //    if (isUpdate && existingParam.Id == paramStructure.Id)
            //    {
            //        return;
            //    }

            //    // 否则抛出领域异常，说明违反了唯一性约束
            //    throw new InvalidOperationException($"Parameter with name '{paramStructure.ParamName}' already exists in the current context.");
            //}
        }

        /// <summary>
        /// 校验关联聚合根：验证 Formula、StandardFamily 等是否真实存在且处于可用状态
        /// </summary>
        private async Task ValidateRelatedAggregatesAsync(ParamStructure paramStructure)
        {
            // 示例1：验证关联的 Formula 聚合根是否存在
            // if (paramStructure.FormulaIds != null && paramStructure.FormulaIds.Any())
            // {
            //     foreach (var formulaId in paramStructure.FormulaIds)
            //     {
            //         var formula = await _formulaRepository.GetByIdAsync(formulaId);
            //         if (formula == null)
            //         {
            //             throw new InvalidOperationException($"Formula with Id '{formulaId}' does not exist.");
            //         }
            //         // 可以校验 Formula 内部的规则：formula.SupportsSchema(paramStructure.Schema)
            //     }
            // }

            // 示例2：验证关联的 StandardFamily 聚合根是否存在
            // if (paramStructure.StandardFamilyIds != null && paramStructure.StandardFamilyIds.Any())
            // {
            //     foreach (var familyId in paramStructure.StandardFamilyIds)
            //     {
            //         var exists = await _standardFamilyRepository.ExistsAsync(familyId);
            //         if (!exists)
            //         {
            //             throw new InvalidOperationException($"StandardFamily with Id '{familyId}' does not exist.");
            //         }
            //     }
            // }

            await Task.CompletedTask; // 占位，待补充实际逻辑后移除
        }
    }
}