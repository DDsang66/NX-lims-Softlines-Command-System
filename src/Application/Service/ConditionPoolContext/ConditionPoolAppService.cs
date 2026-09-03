using Azure.Core;
using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.OrderContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;
using System.Threading;

namespace NX_lims_Softlines_Command_System.src.Application.Service.ConditionPoolContext
{
    public class ConditionPoolAppService:IScopedDependency,IConditionPoolAppService
    {
        private readonly IParamRequireConditionGenerateService _paramRequireConditionGenerateService;
        private readonly IConditionPoolRepository _conditionPoolRepository;
        private readonly IConditionPoolDomainService _conditionPoolDomainService;
        private readonly IUnitOfWork _unitOfWork;

        public ConditionPoolAppService(
            IUnitOfWork unitOfWork, 
            IParamRequireConditionGenerateService paramRequireConditionGenerateService,
            IConditionPoolDomainService conditionPoolDomainService,
            IConditionPoolRepository conditionPoolRepository)
        {
            _unitOfWork = unitOfWork;
            _conditionPoolRepository = conditionPoolRepository;
            _conditionPoolDomainService = conditionPoolDomainService;
            _paramRequireConditionGenerateService = paramRequireConditionGenerateService;
        }

        /// <summary>
        /// 新建草稿状态条件池
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<Guid>> AddConditionPoolAsync(AddConditionPoolDto dto, CancellationToken ct)
        {
            // Implementation for adding a condition pool
            var checklistId = new CheckListId(dto.CheckListId);

            var condition =await  _paramRequireConditionGenerateService.GenerateRequiredConditionsAsync(checklistId, ct);
           
            if (condition.IsFailure||condition.Value == null)
                return Result<Guid>.Fail(condition.Error);

            var conditionPool = ConditionPool.Create(
                checklistId,
                condition.Value);

            await _conditionPoolRepository.AddAsync(conditionPool, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result<Guid>.Ok(conditionPool.Id.Value);
        }

        /// <summary>
        /// 回收前端的输入，更新条件池
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<Guid>> UpdateConditionPoolAsync(UpdateConditionPoolDto dto, CancellationToken ct) 
        {
            var conditionPoolId = new ConditionPoolId(dto.ConditionPoolId);

            var conditionPool = await _conditionPoolRepository.GetByIdAsync(conditionPoolId, ct);

            //可能需要一个中间层处理前端返回的数据和条件池内部condition的字典的映射关系
            var condition = dto.Conditions.ToDictionary(x => x.Key, x => x.Value);

            //调用condition.Update()更新自身_conditions条件字典
            conditionPool.Update(condition);

            await _conditionPoolRepository.UpdateAsync(conditionPool, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result<Guid>.Ok(conditionPool.Id.Value);
        }

        /// <summary>
        /// 获取条件池
        /// </summary>
        /// <param name="conditionPoolId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<ConditionPoolResponseDto>> GetConditionPoolAsync(Guid id, CancellationToken ct) 
        {
            var conditionPool = await _conditionPoolRepository.GetByIdAsync(new ConditionPoolId(id), ct);

            var dto = conditionPool.Adapt<ConditionPoolResponseDto>();

            return Result<ConditionPoolResponseDto>.Ok(dto);
        }

        /// <summary>
        /// 根据前端传入的多个条件池，进行分组，并更新到数据库
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> GroupConditionPoolAsync(
            List<UpdateConditionPoolDto> dto,
            CancellationToken ct)
        {
            // 1. 基础校验
            if (dto == null || dto.Count == 0)
                return Result.Fail("DTO列表不能为空");

            var firstCheckListId = dto[0].CheckListId;
            if (dto.Any(d => d.CheckListId != firstCheckListId))
                return Result.Fail("当前条件池不属于同一个CheckList");

            var checklistId = new CheckListId(firstCheckListId);

            // 2. 查询原始池
            var originalPool = await _conditionPoolRepository
                .GetOriginalPoolByCheckListIdAsync(checklistId, ct);

            if (originalPool == null)
                return Result.Fail($"CheckList {firstCheckListId} 不存在ConditionPool");

            // 3. DTO 转换
            var groupData = dto.Select(d => (
                d.Conditions ?? new Dictionary<string, object?>(),
                d.TestPoints
            )).ToList();

            try
            {
                // 4. 获取现有所有 Pool
                var existingPools = await _conditionPoolRepository
                    .GetByCheckListIdAsync(checklistId, ct);

                // 5. 统一调用领域服务（单分组/多分组逻辑一样）
                var (updated, toUpdate, toCreate, toDelete) = _conditionPoolDomainService.GroupWithReuse(
                    originalPool,
                    existingPools.ToList(),
                    groupData);

                // 6. 执行仓储操作
                foreach (var pool in toDelete)
                    await _conditionPoolRepository.RemoveAsync(pool.Id, ct);

                foreach (var pool in toUpdate)
                    await _conditionPoolRepository.UpdateAsync(pool, ct);

                foreach (var pool in toCreate)
                    await _conditionPoolRepository.AddAsync(pool, ct);

                // 7. 提交事务
                await _unitOfWork.SaveChangesAsync(ct);

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }
    }
}
