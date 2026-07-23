using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repositories;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using System.Runtime.CompilerServices;

namespace NX_lims_Softlines_Command_System.src.Application.Service.ParamGenerateService
{
    public class ParamGenerationUseCaseService :IParamGenerationUseCaseService, IScopedDependency
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IParamStructureRepository _structureRepo;
        private readonly IFormulaRepository _formulaRepo;
        private readonly IParamRuleRepository _ruleRepo;
        private readonly IStandardFamilyRepository _familyRepo;
        private readonly IConditionPoolRepository _conditionPoolRepo;
        private readonly ParamGenerationCoordinator _coordinator;

        public ParamGenerationUseCaseService(
            IUnitOfWork unitOfWork,
            IParamStructureRepository structureRepo,
            IFormulaRepository formulaRepo,
            IParamRuleRepository ruleRepo,
            IStandardFamilyRepository familyRepo,
            IConditionPoolRepository conditionPoolRepo,
            ParamGenerationCoordinator coordinator)
        {
            _unitOfWork = unitOfWork;
            _structureRepo = structureRepo;
            _formulaRepo = formulaRepo;
            _ruleRepo = ruleRepo;
            _familyRepo = familyRepo;
            _conditionPoolRepo = conditionPoolRepo;
            _coordinator = coordinator;
        }

        /// <summary>
        /// 为 CheckList 的某个 Item 生成参数
        /// </summary>
        /// <param name="checkListItemId"></param>
        /// <param name="pool"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<Result<ParamSet>> GenerateForCheckListItemAsync(
            CheckListItem checkListItem,
            ConditionPool pool,
            CancellationToken ct)
        {
            var paramSet = new ParamSet();

            foreach (var standardId in checkListItem.StandardIds)
            {
                // 找到该标准下的所有 ParamStructure
                var family = await _familyRepo.GetByStandardIdAsync(standardId, ct);

                if (family == null) 
                    throw new Exception("未找到标准所属的 Family");

                var structures = await _structureRepo.GetByFamilyIdAsync(family.Id, ct);

                foreach (var structure in structures)
                {
                    var result = await _coordinator.GenerateAsync(structure, pool, ct);
                    if (result.IsSuccess)
                    {
                        paramSet.Merge(result.Value!);
                    }
                }
            }

            var finalParamSet = await _coordinator.FinalGenerateAsync(checkListItem.TestItemId!, paramSet, ct);

            return Result<ParamSet>.Ok(finalParamSet.Value!);
        }

        /// <summary>
        /// 为某个 ParamStructure 生成参数
        /// </summary>
        /// <param name="structureId"></param>
        /// <param name="pool"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<ParamSet>> GenerateForStructureAsync(
            ParamStructureId structureId,
            ConditionPool pool,
            CancellationToken ct)
        {
            var structure = await _structureRepo.GetByIdAsync(structureId, ct);
            if (structure == null) return Result<ParamSet>.Fail("ParamStructure not found");

            return await _coordinator.GenerateAsync(structure, pool, ct);
        }

    }
}
