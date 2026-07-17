using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition
{
    public interface IGenerateRequiredConditionsService:IScopedDependency
    {
        /// <summary>
        /// 构建必填条件
        /// </summary>
        /// <param name="paramStructures"></param>
        /// <returns></returns>
        IDictionary<string, object?> GenerateRequiredConditions(IEnumerable<ParamStructure> paramStructures);

        //            condition格式示例
        //                {
        //                "MachineType": {
        //                    "Type": "System.String",
        //                        "IsRequired": true,
        //                        "AllowedValues": ["Natural", "Synthetic"]
        //                        },
        //                  "Temperature": {
        //                    "Type": "System.Double",
        //                        "IsRequired": true,
        //                        "AllowedValues": [30,40,50,60,70,80,90]
        //                        },
        //                  "WashingProcess": {
        //                    "Type": "System.String",
        //                        "IsRequired": true,
        //                        "AllowedValues": ["Normal", "Gentle", "Mild"]
        //                        }
        //                     }
    }
}
