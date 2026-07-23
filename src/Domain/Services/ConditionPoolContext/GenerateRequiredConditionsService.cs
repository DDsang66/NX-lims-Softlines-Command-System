using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Services.ConditionPoolContext
{
    public class GenerateRequiredConditionsService: IGenerateRequiredConditionsService,IScopedDependency
    {

        /// <summary>
        /// 根据参数结构生成所需的条件字典
        /// 对应1级条件池
        /// </summary>
        /// <param name="paramStructures"></param>
        /// <returns></returns>
        public IDictionary<string, object?> GenerateRequiredConditions(IEnumerable<ParamStructure> paramStructures) 
        {
            var condition = new Dictionary<string, object?>();

            foreach (var paramStructure in paramStructures)
            {
                foreach (var requirement in paramStructure.Schema.ConditionRequirements)
                {
                    // 如果字段已存在，可以选择保留第一个或合并信息
                    if (!condition.ContainsKey(requirement.FieldName))
                    {
                        condition[requirement.FieldName] = new
                        {
                            Type = requirement.FieldName.GetType(),
                            requirement.IsRequired,
                            requirement.AllowedValues
                        };
                    }
                    else
                    {
                        var existing = (dynamic)condition[requirement.FieldName];
                        // 合并AllowedValues
                        var mergedValues = existing.AllowedValues
                            .Concat(requirement.AllowedValues)
                            .Distinct()
                            .ToList();

                        condition[requirement.FieldName] = new
                        {
                            Type = requirement.FieldName.GetType(),
                            IsRequired = requirement.IsRequired || existing.IsRequired,
                            AllowedValues = mergedValues
                        };
                    }
                }
            }

            return condition;
        }
    }
}
