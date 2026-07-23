using ClosedXML.Excel;
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
    public class ConditionPoolValidateService:IConditionPoolValidateService ,IScopedDependency
    {
        public async Task<Result> ValidateConditionPool(ParamStructure structure, Formula formula, ConditionPool pool)
        {
            pool.ChangeToDraft();

            // === 第一层：结构约束校验（字段存在性、白名单）===
            foreach (var requirement in structure.Schema.ConditionRequirements)
            {
                // 存在性
                if (requirement.IsRequired && !pool.HasCondition(requirement.FieldName))
                    return Result.Fail($"Missing required condition: {requirement.FieldName}");

                // 白名单（结构级约束）
                if (pool.HasCondition(requirement.FieldName) &&
                    requirement.AllowedValues != null &&
                    requirement.AllowedValues.Any())
                {
                    var value = pool.GetConditionValue<object>(requirement.FieldName);

                    var valueStr = value.ToString()?.Trim();//转化为字符串比较

                    var result = requirement.AllowedValues
                        .Select(v => v?.ToString()?.Trim())
                        .Any(allowed => string.Equals(allowed, valueStr, StringComparison.OrdinalIgnoreCase));

                    if (!result)
                        return Result.Fail($"Condition '{requirement.FieldName}' value not in allowed list");
                }
            }

            // === 第二层：公式语义校验（表达式、业务规则）===
            // Formula 可能需要的额外校验（如条件组合合法性、表达式求值预检等）
            //var semanticResult = formula.PreValidate(pool);

            //if (!semanticResult.IsSuccess)
            //    return Result.Fail($"Formula semantic validation failed: {semanticResult.Error}");

            pool.ChangeToValidated();

            return Result.Ok();
        }
    }
}
