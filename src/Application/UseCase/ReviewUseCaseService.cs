using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.UseCase
{
    public class ReviewUseCaseService : IScopedDependency
    {
        private readonly ICheckListAppService _checkListAppService;
        private readonly IConditionPoolAppService _conditionPoolAppService;
        private readonly ICheckListRepository _checkListRepository;

        public ReviewUseCaseService(
            ICheckListAppService checkListAppService, 
            IConditionPoolAppService conditionPoolAppService,
            ICheckListRepository checkListRepository)
        {
            _checkListAppService = checkListAppService;
            _conditionPoolAppService = conditionPoolAppService;
            _checkListRepository = checkListRepository;

        }

        /// <summary>
        /// 生成初始工作单和ConditionPool
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<ConditionPoolResponseDto>> GenerateCheckList(AddCheckListDto dto,CancellationToken ct) 
        {
            //新增checklist
            var result = await _checkListAppService.AddCheckList(dto,ct);

            if (result.IsFailure) 
            {
                return Result<ConditionPoolResponseDto>.Fail(result.Error);
            }
            //新增条件池
            var addConditionResult = await  _conditionPoolAppService.AddConditionPoolAsync(
                new AddConditionPoolDto 
                {
                    CheckListId = result.Value,
                    OrderId = dto.SourceId,
                    BuyerCode = dto.BuyerCode,
                    BuyerIsIndividualTraveler = false
                }, ct);

            //将新增条件池的id传入，查询条件池
            var conditionPoolDto = await _conditionPoolAppService.GetConditionPoolAsync(addConditionResult.Value, ct);

            return conditionPoolDto;
        }

        /// <summary>
        /// 执行计算参数
        /// </summary>
        /// <returns></returns>
        public async Task<Result<CheckListResponseDto>> GenerateParam(List<UpdateConditionPoolDto> dto, CancellationToken ct) 
        {
            //对回传测点分组
            var groupResult = await  _conditionPoolAppService.GroupConditionPoolAsync(dto,ct);

            if (groupResult.IsFailure)
            {
                return Result<CheckListResponseDto>.Fail(groupResult.Error);
            }
            //根据checklistid计算参数
            var checklistId = dto[0].CheckListId;

            await _checkListAppService.CalculateParamAsync(checklistId, ct);

            var checklist = await  _checkListAppService.GetCheckListAsync(checklistId, ct);

            return checklist;
        }
    }
} 