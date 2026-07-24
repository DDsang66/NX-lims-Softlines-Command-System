using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.CheckListContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repositories;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using System.Reflection.Emit;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace NX_lims_Softlines_Command_System.src.Application.Service.CheckListContext
{
    public class CheckListAppService:IScopedDependency,ICheckListAppService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICheckListRepository _checkListRepository;
        private readonly IConditionPoolRepository  _conditionPoolRepository;
        private readonly IParamGenerationUseCaseService _paramGenerationUseCaseService;

        public CheckListAppService(
            IUnitOfWork unitOfWork, 
            ICheckListRepository checkListRepository,
            IConditionPoolRepository conditionPoolRepository,
            IParamGenerationUseCaseService paramGenerationUseCaseService)
        {
            _unitOfWork = unitOfWork;
            _checkListRepository = checkListRepository;
            _conditionPoolRepository = conditionPoolRepository;
            _paramGenerationUseCaseService = paramGenerationUseCaseService;
        }

        /// <summary>
        /// 添加测试清单
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> AddCheckList(AddCheckListDto dto,CancellationToken ct) 
        {
            var checkList = dto.Adapt<CheckList>();//已在Mapping调用工厂方法统一创建

            Console.WriteLine(checkList);

            await _checkListRepository.AddAsync(checkList, ct);

            await  _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 更新测试清单
        /// </summary>
        /// <param name="dto"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> UpdateCheckList(UpdateCheckListDto dto, CancellationToken ct)
        {
            var checkList = await _checkListRepository.GetByIdAsync(new CheckListId(dto.Id), ct);

            checkList.Update();

            await _checkListRepository.UpdateAsync(checkList, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }

        /// <summary>
        /// 获取测试清单
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<CheckListResponseDto>> GetCheckListAsync(Guid checkListId, CancellationToken ct) 
        {
            var checkList = await _checkListRepository.GetByIdAsync(new CheckListId(checkListId), ct);

            var checkListDto = checkList.Adapt<CheckListResponseDto>();

            return Result<CheckListResponseDto>.Ok(checkListDto);
        }

        /// <summary>
        /// 计算参数
        /// </summary>
        /// <param name="checkListId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> CalculateParamAsync(Guid id, CancellationToken ct) 
        {
            var checkListId = new CheckListId(id);

            var checkList = await _checkListRepository.GetByIdAsync(checkListId, ct);

            var checkListItems = checkList.GetTestItem(); // 通过聚合根获取内部实体
            if (checkListItems == null)
                return Result.Fail("未能找到测试项目");

            // 2. 获取与该检查项关联的所有条件池（假设已经分组完毕）
            var existingPools = await _conditionPoolRepository.GetByCheckListIdAsync(checkListId, ct);

            // 3. 为每个测试项生成参数
            foreach (var item in checkListItems)
            {
                // 创建新的参数字典
                var TestPointParams = new Dictionary<string, ParamSet?>();

                // 遍历每个测点
                foreach (var testPoint in item.Samples)
                {
                    // 找到该测点对应的条件池
                    var pool = existingPools.FirstOrDefault(p => p.TestPoints.Contains(testPoint));

                    // 使用单个条件池生成参数
                    var result = await _paramGenerationUseCaseService.GenerateForCheckListItemAsync( item, pool, ct);

                    if (!result.IsSuccess)
                        return Result.Fail($"生成测试项 {item.Id} 的测点 {testPoint} 参数时发生错误: {result.Error}");
                   
                    // 将生成的参数添加到新字典中
                    TestPointParams.Add(testPoint, result.Value);
                }

                // 更新测试项的参数
                item.TestPointParams = TestPointParams;
            }

            // 4. 保存更改
            checkList.Update();

            await _checkListRepository.UpdateAsync(checkList, ct);

            await _unitOfWork.SaveChangesAsync(ct);

            return Result.Ok();
        }
    }
}
