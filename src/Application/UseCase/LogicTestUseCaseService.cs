using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.UseCase;
using NX_lims_Softlines_Command_System.src.Application.Service.ParamGenerateService;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repositories;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.UseCase
{
    public class LogicTestUseCaseService:IScopedDependency
    {
        private readonly IConditionPoolRepository _conditionPoolRepository;
        private readonly IConditionPoolDomainService _conditionPoolDomainService;
        private readonly IParamStructureRepository _paramStructureRepository;
        private readonly ParamGenerationCoordinator _coordinator;
        private readonly IUnitOfWork _unitOfWork;

        public LogicTestUseCaseService(
            IConditionPoolRepository conditionPoolRepository,
            IConditionPoolDomainService conditionPoolDomainService, 
            IParamStructureRepository paramStructureRepository,
            ParamGenerationCoordinator coordinator,
            IUnitOfWork unitOfWork)
        {
            _conditionPoolRepository = conditionPoolRepository;
            _conditionPoolDomainService = conditionPoolDomainService;
            _paramStructureRepository = paramStructureRepository;
            _coordinator = coordinator;
            _unitOfWork = unitOfWork;
        }
        /// <summary>
        /// 回收前端的输入，更新条件池
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<ConditionPoolResponseDto>> UpdateConditionPoolAsync(LogicTestConditionUpdateDto dto, CancellationToken ct)
        {
            var conditionPoolId = new ConditionPoolId(dto.ConditionPoolId);

            var conditionPool = await _conditionPoolRepository.GetByIdAsync(conditionPoolId, ct);

            //可能需要一个中间层处理前端返回的数据和条件池内部condition的字典的映射关系
            var condition = dto.Conditions.ToDictionary(x => x.Key, x => x.Value);

            //调用condition.Update()更新自身_conditions条件字典
            conditionPool.Update(condition);

            await _conditionPoolRepository.UpdateAsync(conditionPool, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            var responseDto = conditionPool.Adapt<ConditionPoolResponseDto>();

            return Result<ConditionPoolResponseDto>.Ok(responseDto);
        }

        /// <summary>
        /// 根据前端传入的条件池，执行match计算返回result
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<ParamSetDto>> TestLogicAsync(TestLogicSubmitDto dto, CancellationToken ct) 
        {
            var conditionPoolId = new ConditionPoolId(dto.ConditionPoolId);

            var conditionPool = await _conditionPoolRepository.GetByIdAsync(conditionPoolId, ct);

            var paramStructures = await  _paramStructureRepository.GetByFormulaIdsAsync(dto.FormulaIds.Select(id => new FormulaId(id)).ToList(), ct);

            var paramSet = new ParamSet();

            foreach (var paramStructure in paramStructures)
            {
                var result = await _coordinator.GenerateAsync(paramStructure, conditionPool, ct);
                if (result.IsSuccess)
                {
                    paramSet.Merge(result.Value!);
                }
            }

            var dtoResult = paramSet.Adapt<ParamSetDto>();

            return Result<ParamSetDto>.Ok(dtoResult);
        }

    }
}
