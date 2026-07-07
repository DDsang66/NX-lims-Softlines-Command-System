using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.NX_lims_Softlines_Command_System.src.Application.ParamEngineContext.Dtos;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;


namespace NX_lims_Softlines_Command_System.src.Application.Service.ParamRuleAppService
{
    public class ParamRuleApplicationService: IParamRuleApplicationService,IScopedDependency
    {
        private readonly IParamRuleRepository _repository;
        private readonly IConditionPatternDirectorService _patternDirector;

        public ParamRuleApplicationService(
            IParamRuleRepository repository,
            IConditionPatternDirectorService patternDirector)
        {
            _repository = repository;
            _patternDirector = patternDirector;
        }

        /// <summary>
        /// 创建参数规则
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ParamRuleDto> CreateParamRuleAsync(CreateParamRuleRequest request,CancellationToken ct)
        {
            // 1. 使用Director进行转换
            var pattern = _patternDirector.CreatePatternFromDto(request);

            // 2. 创建聚合根
            var rule = ParamRule.Create(
                new ParamRuleId(request.Id),
                new FormulaId(request.FormulaId),
                request.ParamName,
                request.Priority,
                pattern
            );

            // 3. 激活规则(待其余聚合根逻辑完善后用单独的方法激活)
            rule.Active();

            // 4. 持久化
            await _repository.AddAsync(rule,ct);

            // 5. 返回DTO
            return rule.Adapt<ParamRuleDto>();
        }

        /// <summary>
        /// 更新参数规则
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<ParamRuleDto> UpdateParamRuleAsync(UpdateParamRuleRequest request,CancellationToken ct)
        {
            // 1. 获取现有规则
            var existingRule = await _repository.GetByIdAsync(new ParamRuleId(request.Id),ct);

            if (existingRule == null)
                throw new Exception($"Param rule with id {request.Id} not found");

            var changedRequest = request.Adapt<CreateParamRuleRequest>();

            // 2. 使用Director进行转换
            var pattern = _patternDirector.CreatePatternFromDto(changedRequest);

            // 3. 更新规则
            existingRule.ChangePriority(request.Priority);

            //existingRule.Pattern = pattern; // 注意：这里通过聚合根方法去更新

            // 4. 持久化
            await _repository.UpdateAsync(existingRule,ct);

            // 5. 返回DTO
            return existingRule.Adapt<ParamRuleDto>();
        }

        /// <summary>
        /// 获取参数规则
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<ParamRuleDto> GetParamRuleAsync(string id,CancellationToken ct)
        {
            var rule = await _repository.GetByIdAsync(new ParamRuleId(id), ct);
            if (rule == null)
                throw new Exception($"Param rule with id {id} not found");

            return rule.Adapt<ParamRuleDto>();
        }
    }
}
