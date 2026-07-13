using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository;

namespace NX_lims_Softlines_Command_System.src.Application.Service.ConditionPoolContext
{
    public class ParamRequireConditionGenerateService: IScopedDependency, IParamRequireConditionGenerateService
    {
        private readonly IParamStructureRepository _paramStructureRepository;
        private readonly ICheckListRepository _checkListRepository;
        //private readonly IOrderRepository _orderRepository;
        private readonly IStandardFamilyRepository _standardFamilyRepository;

        public ParamRequireConditionGenerateService(
            IParamStructureRepository paramStructureRepository, 
            IStandardFamilyRepository standardFamilyRepository,
            ICheckListRepository checkListRepository)
        {
            _paramStructureRepository = paramStructureRepository;
            _standardFamilyRepository = standardFamilyRepository;
            _checkListRepository = checkListRepository;
        }

        /// <summary>
        /// 协调构造Condition字典
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<IDictionary<string, object?>>> GenerateRequiredConditionsAsync(CheckListId checklistid,CancellationToken ct)
        {
            //获取已经生成的Checklist
            var checklist = await _checkListRepository.GetByIdAsync(checklistid, ct);

            var standardIds = new List<StandardId>();

            //循环查询checklist中的所有项目-标准
            foreach (var item in checklist.Items)
            {
                foreach (var standardId in item.StandardIds)
                {
                    standardIds.Add(standardId);
                }
            }

            var standardFamilyIds = new List<StandardFamilyId>();

            //获取所有标准对应的标准族
            foreach (var standardId in standardIds) 
            {
                var standardFamily = await _standardFamilyRepository.GetByStandardIdAsync(standardId, ct);
              
                if (standardFamily != null)
                {
                    standardFamilyIds.Add(standardFamily.Id);
                }
            }

            //获取标准族对应的结构
            var paramStructures = new List<ParamStructure>();

            foreach (var standardFamilyId in standardFamilyIds)
            {
                var paramStructure = await _paramStructureRepository.GetByFamilyIdAsync(standardFamilyId, ct);
                if (paramStructure != null) 
                {
                    paramStructures.AddRange(paramStructure);
                }
            }

            var condition = new Dictionary<string, object?>();

            // 从 ConditionRequirements 获取条件字段

            //提取结构(ParamStructure)中的ConditionField[ConditionName,Type,allowedValues]等数据

            //构建Condition字典对象[conditionName作为string，Type用于规范object类型]
            foreach (var paramStructure in paramStructures)
            {
                foreach (var requirement in paramStructure.Schema.ConditionRequirements)
                {
                    condition[requirement.FieldName] = new
                    {
                        Type = requirement.FieldType,
                        IsRequired = requirement.IsRequired,
                        AllowedValues = requirement.AllowedValues
                    };
                }
            }
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
//                        "AllowedValues": []
//                        },
//                  "WashingProcess": {
//                    "Type": "System.String",
//                        "IsRequired": false,
//                        "AllowedValues": ["Normal", "Gentle", "Heavy"]
//                        }
//                     }

            return Result<IDictionary<string, object?>>.Ok(condition);
        }
    }
}
