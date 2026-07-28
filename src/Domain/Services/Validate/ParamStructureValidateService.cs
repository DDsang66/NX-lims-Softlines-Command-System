using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Services.Validate
{
    public class ParamStructureValidateService: IScopedDependency
    {
        /// <summary>
        /// 原子校验：Formula 与 ParamStructure 语义关联与成员关系校验
        /// - Formula.ParamName 与 ParamStructure.ParamName 要一致（忽略大小写）
        /// - ParamStructure.Id 必须包含在 Formula.ParamSturctureIds 中（若公式声明了该关联）
        /// - ParamStructure 所属 StandardFamily（若存在）应至少有一个与 Formula.StandardFamilyIds 重合（可选校验）
        /// </summary>
        public Result ValidateStructureAssociation(Formula formula, ParamStructure structure)
        {
            if (!string.Equals(formula.ParamName, structure.ParamName, StringComparison.OrdinalIgnoreCase))
                return Result.Fail("Formula.ParamName does not match ParamStructure.ParamName");

            if (structure.Id != null)
            {
                var contains = formula.ParamStructureIds != null &&
                    formula.ParamStructureIds.Any(ps => ps != null && ps.Equals(structure.Id));
                if (!contains)
                    return Result.Fail("Formula does not include the given ParamStructureId");
            }

            // 可选：验证标准族重合（若结构或公式声明了标准族）
            var structureFamilies = structure.StandardFamilyIds ?? Enumerable.Empty<StandardFamilyId?>();
            var formulaFamilies = formula.StandardFamilyIds ?? Enumerable.Empty<StandardFamilyId?>();
            if (structureFamilies.Any() && formulaFamilies.Any())
            {
                var overlap = structureFamilies
                    .Where(sf => sf != null)
                    .Select(sf => sf!.Value)
                    .Intersect(formulaFamilies.Where(ff => ff != null).Select(ff => ff!.Value))
                    .Any();

                if (!overlap)
                    return Result.Fail("No overlapping StandardFamily between Formula and ParamStructure");
            }

            return Result.Ok();
        }

        /// <summary>
        /// 原子校验：ParamStructure 是否覆盖 Formula 所需的 condition requirements
        /// - ParamStructure.Schema.ConditionRequirements 必须被 Formula.ConditionFields 覆盖
        /// </summary>
        public Result ValidateConditionRequirementsCoverage(Formula formula, ParamStructure structure)
        {
            var required = structure.Schema?.ConditionRequirements?
                .Where(cr => !string.IsNullOrWhiteSpace(cr.FieldName))
                .Select(cr => cr.FieldName!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();

            var formulaFields = (formula.ConditionFields ?? Enumerable.Empty<string>())
                .Select(f => f?.Trim())
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var missing = required
                .Where(r => !formulaFields.Contains(r, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (missing.Any())
                return Result.Fail("Formula does not include required condition fields declared by ParamStructure", details: missing);

            return Result.Ok();
        }

        /// <summary>
        /// 组合校验：按常见流程依次执行原子校验（应用层也可以按需只调用原子方法）
        /// </summary>
        public Result Validate(Formula formula, ParamStructure structure)
        {
            if (structure != null)
            {
                var r = ValidateStructureAssociation(formula, structure);
                if (r.IsFailure) return r;

                r = ValidateConditionRequirementsCoverage(formula, structure);
                if (r.IsFailure) return r;
            }

            return Result.Ok();
        }

    }
}