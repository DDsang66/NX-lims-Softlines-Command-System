using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine
{
    public interface IParamStructureValidateService:IScopedDependency
    {
        /// <summary>
        /// 原子校验：Formula 与 ParamStructure 语义关联与成员关系校验
        /// </summary>
        /// <param name="formula"></param>
        /// <param name="structure"></param>
        /// <returns></returns>
        Result Validate(Formula formula, ParamStructure structure);

        /// <summary>
        /// 是否覆盖所有 condition requirements
        /// </summary>
        /// <param name="formula"></param>
        /// <param name="structure"></param>
        /// <returns></returns>
        Result ValidateConditionRequirementsCoverage(Formula formula, ParamStructure structure);

        /// <summary>
        /// - Formula.ParamName 与 ParamStructure.ParamName 要一致（忽略大小写）
        /// - ParamStructure.Id 必须包含在 Formula.ParamSturctureIds 中（若公式声明了该关联）
        /// - ParamStructure 所属 StandardFamily（若存在）应至少有一个与 Formula.StandardFamilyIds 重合（可选校验）
        /// </summary>
        /// <param name="formula"></param>
        /// <param name="structure"></param>
        /// <returns></returns>
        Result ValidateStructureAssociation(Formula formula, ParamStructure structure);
    }
}
