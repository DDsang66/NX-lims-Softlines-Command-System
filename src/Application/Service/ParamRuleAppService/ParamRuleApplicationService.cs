using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.NX_lims_Softlines_Command_System.src.Application.ParamEngineContext.Dtos;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.Enums;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using Spire.AI.Api;

namespace NX_lims_Softlines_Command_System.src.Application.Service.ParamRuleAppService
{
    public interface IParamRuleApplicationService: IScopedDependency
    {
        Task<ParamRuleDto> CreateParamRuleAsync(CreateParamRuleRequest request);
        Task<ParamRuleDto> UpdateParamRuleAsync(UpdateParamRuleRequest request);
        Task<ParamRuleDto> GetParamRuleAsync(string id);
    }

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
        public async Task<ParamRuleDto> CreateParamRuleAsync(CreateParamRuleRequest request)
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

            // 3. 激活规则
            rule.Active();

            // 4. 持久化
            await _repository.AddAsync(rule);

            // 5. 返回DTO
            return MapToDto(rule);
        }

        /// <summary>
        /// 更新参数规则
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<ParamRuleDto> UpdateParamRuleAsync(UpdateParamRuleRequest request)
        {
            // 1. 获取现有规则
            var existingRule = await _repository.FindAsync(new ParamRuleId(request.Id));

            if (existingRule == null)
                throw new Exception($"Param rule with id {request.Id} not found");

            var changedRequest = request.Adapt<CreateParamRuleRequest>();

            // 2. 使用Director进行转换
            var pattern = _patternDirector.CreatePatternFromDto(changedRequest);

            // 3. 更新规则
            existingRule.ChangePriority(request.Priority);

            //existingRule.Pattern = pattern; // 注意：这里通过聚合根方法去更新

            // 4. 持久化
            await _repository.UpdateAsync(existingRule);

            // 5. 返回DTO
            return MapToDto(existingRule);
        }

        /// <summary>
        /// 获取参数规则
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<ParamRuleDto> GetParamRuleAsync(string id)
        {
            var rule = await _repository.FindAsync(new ParamRuleId(id));
            if (rule == null)
                throw new Exception($"Param rule with id {id} not found");

            return MapToDto(rule);
        }

        private ParamRuleDto MapToDto(ParamRule rule)
        {
            return new ParamRuleDto
            {
                Id = rule.Id.Value,
                FormulaId = rule.FormulaId.Value,
                ParamName = rule.ParamName,
                Priority = rule.Priority,
                IsActive = rule.IsActive
            };
        }
    }
}
