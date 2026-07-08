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
        private readonly IParamRuleRepository _pararmRuleRepository;
        private readonly IRuleTranslationService _ruleTranslationService;
        private readonly IFormulaRepository _formulaRepository;

        public ParamRuleApplicationService(
            IParamRuleRepository pararmRuleRepository,
            IRuleTranslationService ruleTranslationService,
            IFormulaRepository formulaRepository)
        {
            _pararmRuleRepository = pararmRuleRepository;
            _ruleTranslationService = ruleTranslationService;
            _formulaRepository = formulaRepository;
        }

        /// <summary>
        /// 创建参数规则
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<ParamRuleDto> AddParamRuleFromJsonAsync(CreateParamRuleRequest request,CancellationToken ct)
        {
            // 1. 使用Director进行转换
            var pattern = _ruleTranslationService.PatternTranslateFromDto(request,ct);

            // 2. 创建聚合根
            var rule = ParamRule.Create(
                new ParamRuleId(request.Id),
                new FormulaId(request.FormulaId),
                request.ParamName,
                request.Priority,
                pattern,
                new ParamValue(request.ParamResult)
            );

            // 3. 激活规则(待其余聚合根逻辑完善后用单独的方法激活)
            rule.Active();

            // 4. 持久化
            await _pararmRuleRepository.AddAsync(rule,ct);

            // 5. 返回DTO
            return rule.Adapt<ParamRuleDto>();
        }

        /// <summary>
        /// 自然语言规则添加
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<ParamRuleDto> AddParamRuleFromNaturalTextAsync(NaturalLanguageRuleRequest request, CancellationToken ct)
        {
            // 1. 获取公式
            var formula = await _formulaRepository.GetByIdAsync(new FormulaId(request.FormulaId),ct);

            // 2. 使用Director进行转换
            var (pattern, result) = _ruleTranslationService.ParseFromNaturalLanguageText(request.Text, formula, ct);

            // 3. 创建聚合根
            var rule = ParamRule.Create(
                new ParamRuleId(request.FormulaId),
                new FormulaId(request.FormulaId),
                request.ParamName,
                request.Priority,
                pattern: pattern,
                result: result
            );

            // 4. 激活规则(待其余聚合根逻辑完善后用单独的方法激活)
            rule.Active();

            // 5. 持久化
            await _pararmRuleRepository.AddAsync(rule, ct);

            // 6. 返回DTO
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
            var existingRule = await _pararmRuleRepository.GetByIdAsync(new ParamRuleId(request.Id),ct);

            if (existingRule == null)
                throw new Exception($"Param rule with id {request.Id} not found");

            var changedRequest = request.Adapt<CreateParamRuleRequest>();

            // 2. 使用Director进行转换
            //var pattern = _ruleTranslationService.TranslateFromDto(request, ct);

            // 3. 更新规则
            existingRule.ChangePriority(request.Priority);

            //existingRule.Pattern = pattern; // 注意：这里通过聚合根方法去更新

            // 4. 持久化
            await _pararmRuleRepository.UpdateAsync(existingRule,ct);

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
            var rule = await _pararmRuleRepository.GetByIdAsync(new ParamRuleId(id), ct);
            if (rule == null)
                throw new Exception($"Param rule with id {id} not found");

            return rule.Adapt<ParamRuleDto>();
        }
    }
}
