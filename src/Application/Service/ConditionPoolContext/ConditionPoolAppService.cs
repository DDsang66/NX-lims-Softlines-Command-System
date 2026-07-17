using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repositories;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.ConditionPoolContext
{
    public class ConditionPoolAppService:IScopedDependency
    {
        private readonly IParamRequireConditionGenerateService _paramRequireConditionGenerateService;
        private readonly IConditionPoolRepository _conditionPoolRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ConditionPoolAppService(
            IUnitOfWork unitOfWork, 
            IParamRequireConditionGenerateService paramRequireConditionGenerateService,
            IConditionPoolRepository conditionPoolRepository)
        {
            _unitOfWork = unitOfWork;
            _conditionPoolRepository = conditionPoolRepository;
            _paramRequireConditionGenerateService = paramRequireConditionGenerateService;
        }

        /// <summary>
        /// 新建草稿状态条件池
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> AddConditionPoolAsync(AddConditionPoolDto dto, CancellationToken ct)
        {
            // Implementation for adding a condition pool
            var checklistId = new CheckListId(dto.CheckListId);

            var condition =await  _paramRequireConditionGenerateService.GenerateRequiredConditionsAsync(checklistId, ct);
           
            if (condition.IsFailure||condition.Value == null)
                return Result.Fail(condition.Error);

            var conditionPool = ConditionPool.Create(
                new ConditionPoolId(Guid.NewGuid()),
                new OrderId(dto.OrderId),
                checklistId,
                condition.Value);

            // Save changes to the database

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 回收前端的输入，更新条件池
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> UpdateConditionPoolAsync(UpdateConditionPoolDto dto, CancellationToken ct) 
        {
            var conditionPoolId = new ConditionPoolId(dto.ConditionPoolId);

            var conditionPool = await _conditionPoolRepository.GetByIdAsync(conditionPoolId, ct);

            //可能需要一个中间层处理前端返回的数据和条件池内部condition的字典的映射关系
            var condition = dto.Conditions.ToDictionary(x => x.Key, x => x.Value);

            //调用condition.Update()更新自身_conditions条件字典
            conditionPool.Update(condition);

            await _conditionPoolRepository.UpdateAsync(conditionPool, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }
    }
}
