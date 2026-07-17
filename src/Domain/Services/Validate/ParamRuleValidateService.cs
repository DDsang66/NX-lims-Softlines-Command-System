using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Services.Validate
{
    public class ParamRuleValidateService: IScopedDependency,IParamRuleValidateService
    {
        /// <summary>
        /// 绑定一致性：ParamRule 的 FormulaId 必须与 Formula 的 Id 相同
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="formula"></param>
        /// <returns></returns>
        public Result ValidateBinding(ParamRule rule, Formula formula)
        {
            if (rule == null) return Result.Fail("ParamRule is null");
            if (formula == null) return Result.Fail("Formula is null");

            if (rule.FormulaId != null && !rule.FormulaId.Equals(formula.Id))
                return Result.Fail("ParamRule is associated with a different Formula");

            return Result.Ok();
        }

        /// <summary>
        /// 字段一致性：ParamRule 的 RequiredConditions 必须包含在 Formula 的 RequiredConditions 中
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="formula"></param>
        /// <returns></returns>
        public Result ValidateFieldsAgainstFormula(ParamRule rule, Formula formula)
        {
            if (rule == null) return Result.Fail("ParamRule is null");
            if (formula == null) return Result.Fail("Formula is null");

            var ruleFields = rule.Pattern?.RequiredConditions() ?? Enumerable.Empty<string>();
            var formulaFields = formula.RequiredConditions() ?? Enumerable.Empty<string>();

            var missing = ruleFields
                .Where(f => !formulaFields.Contains(f, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (missing.Any())
                return Result.Fail("ParamRule references condition fields not declared by Formula", details: missing);

            return Result.Ok();
        }

        /// <summary>
        /// 结构一致性：ParamRule 的 ParamStructureId 必须包含在 Formula 的 ParamSturctureIds 中
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="formula"></param>
        /// <returns></returns>
        public Result ValidateStructureMembership(ParamRule rule, Formula formula)
        {
            if (rule == null) return Result.Fail("ParamRule is null");
            if (formula == null) return Result.Fail("Formula is null");

            if (rule.StructureId != null)
            {
                var containsStructure = formula.ParamSturctureIds != null &&
                    formula.ParamSturctureIds.Any(ps => ps != null && ps.Equals(rule.StructureId));
                if (!containsStructure)
                    return Result.Fail("ParamRule's ParamStructureId is not included in Formula");
            }

            return Result.Ok();
        }

        /// <summary>
        /// 校验一致性：ParamRule 的结果值必须符合 ParamStructure 的限制
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="structure"></param>
        /// <returns></returns>
        public Result ValidateResultCompatibility(ParamRule rule, ParamStructure structure)
        {
            if (rule == null) return Result.Fail("ParamRule is null");
            if (structure == null) return Result.Fail("ParamStructure is null");

            var schema = structure.Schema;
            if (schema == null) return Result.Fail("ParamStructure.Schema is null");

            // 语义一致性：ParamName 必须一致
            if (!string.Equals(rule.FormulaId == null ? string.Empty : rule.FormulaId.ToString(), string.Empty)
                && !string.Equals(structure.ParamName, structure.ParamName, StringComparison.OrdinalIgnoreCase))
            {
                // 这里不强校验 Formula.ParamName —— 应用层若需可额外调用
            }

            var resultVal = rule.GetResult()?.Value;

            if (resultVal == null && !schema.RequiredParam.IsNullable)
                return Result.Fail($"ParamRule result is null but target param '{schema.RequiredParam.Name}' is not nullable");

            var mainName = schema.RequiredParam.Name;
            schema.Limitations.TryGetValue(mainName, out var limitation);
            var expectedType = schema.RequiredParam.ValueType;

            if (limitation != null)
            {
                var ok = limitation.IsValid(resultVal, expectedType);
                if (!ok)
                    return Result.Fail($"ParamRule result value is not valid under ParamStructure limitations for '{mainName}'");
            }
            else
            {
                if (resultVal != null && expectedType != null)
                {
                    try
                    {
                        if (expectedType.IsEnum)
                        {
                            if (!Enum.TryParse(expectedType, resultVal.ToString(), true, out var _))
                                return Result.Fail($"ParamRule result cannot be converted to enum {expectedType.Name}");
                        }
                        else
                        {
                            _ = Convert.ChangeType(resultVal, expectedType);
                        }
                    }
                    catch
                    {
                        return Result.Fail($"ParamRule result cannot be converted to expected type {expectedType.Name}");
                    }
                }
            }

            // 检查 ParamStructure 要求的 condition fields 是否被规则覆盖
            // 具体通过字段名称
            var ruleFields = rule.Pattern?.RequiredConditions() ?? Enumerable.Empty<string>();
            var missingReqs = schema.ConditionRequirements
                .Where(cr => !string.IsNullOrWhiteSpace(cr.FieldName))
                .Select(cr => cr.FieldName!)
                .Where(fn => !ruleFields.Contains(fn, StringComparer.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (missingReqs.Any())
                return Result.Fail("ParamRule does not provide required condition fields as declared by ParamStructure", details: missingReqs);

            return Result.Ok();
        }

        /// <summary>
        /// 薄包装：按顺序执行原子校验（保留以兼容历史调用，推荐应用层自行编排）
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="formula"></param>
        /// <param name="structure"></param>
        /// <returns></returns>
        public Result Validate(ParamRule rule, Formula formula, ParamStructure? structure = null)
        {
            var r = ValidateBinding(rule, formula);
            if (r.IsFailure) return r;

            r = ValidateFieldsAgainstFormula(rule, formula);
            if (r.IsFailure) return r;

            r = ValidateStructureMembership(rule, formula);
            if (r.IsFailure) return r;

            if (structure != null)
            {
                r = ValidateResultCompatibility(rule, structure);
                if (r.IsFailure) return r;
            }

            return Result.Ok();
        }
    }
}

