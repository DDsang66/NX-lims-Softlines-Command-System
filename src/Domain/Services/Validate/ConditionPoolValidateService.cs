using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Services.Validate
{
    /// <summary>
    /// 条件池验证服务
    /// </summary>
    public class ConditionPoolValidateService: IConditionPoolValidateService,IScopedDependency
    {
        /// <summary>
        /// 验证一级条件池是否满足结构要求（结构层面）
        /// - 只做字段存在性、白名单基础校验
        /// - 更复杂的语义校验（表达式、数据类型细化）由 Formula/语义分析器完成
        /// </summary>
        public async Task<Result> EnsureConditionPoolConformance(ParamStructure structure, ConditionPool pool)
        {
            if (structure == null)
                return Result.Fail("ParamStructure is null");

            if (pool == null) 
                return Result.Fail("ConditionPool is null");

            foreach (var requirement in structure.Schema.ConditionRequirements)
            {
                // 存在性验证
                if (requirement.IsRequired && !pool.HasCondition(requirement.FieldName))
                    return Result.Fail($"Missing required condition: {requirement.FieldName}");

                // 白名单验证
                if (pool.HasCondition(requirement.FieldName) &&
                    requirement.AllowedValues != null &&
                    requirement.AllowedValues.Any())
                {
                    var value = pool.GetConditionValue<object>(requirement.FieldName);

                    if (!requirement.AllowedValues.Contains(value))
                        return Result.Fail($"Condition '{requirement.FieldName}' has invalid value");
                }
            }

            return Result.Ok();
        }

        /// <summary>
        /// 验证二级条件池是否满足公式要求（公式层面）
        /// </summary>
        /// <param name="formula"></param>
        /// <param name="pool"></param>
        /// <returns></returns>
        public async Task<Result> EnsureConditionPoolWithFormula(Formula formula, ConditionPool pool) 
        {
            if (formula == null) 
                return Result.Fail("Formula is null");

            if (pool == null)
                return Result.Fail("ConditionPool is null");

            var missing = formula.RequiredConditions().Where(f => !pool.HasCondition(f)).ToList();

            if (missing.Any())
                return Result.Fail($"Missing required conditions: {string.Join(',', missing)}");

            return Result.Ok();
        }
    }
}
