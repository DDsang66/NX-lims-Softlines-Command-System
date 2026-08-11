using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;


namespace NX_lims_Softlines_Command_System.src.Application.Service.ParamRuleAppService
{
    public class ParamRuleApplicationService: IParamRuleApplicationService,IScopedDependency
    {
        /// <summary>
        /// 仓储
        /// </summary>
        private readonly IParamRuleRepository _pararmRuleRepository;

        /// <summary>
        /// 规则自然语言翻译服务
        /// </summary>
        private readonly IRuleTranslationService _ruleTranslationService;

        /// <summary>
        /// 公式仓储
        /// </summary>
        private readonly IFormulaRepository _formulaRepository;

        private readonly IParamRuleValidateService _paramRuleValidateService;

        private readonly IParamStructureRepository _paramStructureRepository;

        /// <summary>
        /// 工作单元
        /// </summary>
        private readonly IUnitOfWork _unitOfWork;

        public ParamRuleApplicationService(
            IParamRuleRepository pararmRuleRepository,
            IParamRuleValidateService paramRuleValidateService,
            IRuleTranslationService ruleTranslationService,
            IFormulaRepository formulaRepository,
            IParamStructureRepository paramStructureRepository,
            IUnitOfWork unitOfWork)
        {
            _pararmRuleRepository = pararmRuleRepository;
            _paramRuleValidateService = paramRuleValidateService;
            _paramStructureRepository = paramStructureRepository;
            _ruleTranslationService = ruleTranslationService;
            _formulaRepository = formulaRepository;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// 创建参数规则
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<Result> AddParamRuleFromJsonAsync(CreateParamRuleRequest request,CancellationToken ct)
        {
            // 1. 使用Director进行转换
            var pattern = _ruleTranslationService.PatternTranslateFromDto(request,ct);

            // 2. 创建聚合根
            var rule = ParamRule.Create(
                new ParamRuleId(request.Id),
                new FormulaId(request.FormulaId),
                new ParamStructureId(request.ParamStructureId),
                request.ParamName,
                request.Priority,
                pattern,
                new ParamValue(request.ParamResult)
            );

            await _pararmRuleRepository.AddAsync(rule,ct);

            await _unitOfWork.SaveChangesAsync();

            return Result.Ok();
        }

        /// <summary>
        /// 自然语言规则添加
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> AddParamRuleFromNaturalTextAsync(NaturalLanguageRuleRequest request, CancellationToken ct)
        {
            // 1. 获取公式
            var formula = await _formulaRepository.GetByIdAsync(new FormulaId(request.FormulaId),ct);
            if (formula == null) return Result.Fail($"Formula {request.FormulaId} 不存在");

            // 2. 使用Director进行转换
            var (pattern, result) = _ruleTranslationService.ParseFromNaturalLanguageText(request.Text, formula, ct);

            // 3. 创建聚合根（ParamStructureId 可为空，与 ParamRule.Create 的可空设计一致）
            var rule = ParamRule.Create(
                new ParamRuleId(request.Id),
                new FormulaId(request.FormulaId),
                string.IsNullOrWhiteSpace(request.ParamStructureId) ? null : new ParamStructureId(request.ParamStructureId),
                request.ParamName,
                request.Priority,
                pattern: pattern,
                result: result
            );

            await  _pararmRuleRepository.AddAsync(rule, ct);

            await _unitOfWork.SaveChangesAsync();

            return Result.Ok();
        }

        /// <summary>
        /// 更新参数规则
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<Result> UpdateParamRuleWithJsonAsync(UpdateParamRuleJsonRequest request,CancellationToken ct)
        {
            // 1. 获取现有规则
            var existingRule = await _pararmRuleRepository.GetByIdAsync(new ParamRuleId(request.Id),ct);

            if (existingRule == null)
                throw new Exception($"Param rule with id {request.Id} not found");

            var changedRequest = request.Adapt<CreateParamRuleRequest>();

            var pattern = _ruleTranslationService.PatternTranslateFromDto(changedRequest, ct);

            // 3. 更新规则
            existingRule.Update(pattern, changedRequest.ParamResult, changedRequest.Priority, changedRequest.StopOnMatch);

            // 4. 持久化
            await _pararmRuleRepository.UpdateAsync(existingRule,ct);

            await _unitOfWork.SaveChangesAsync();

            // 5. 返回
            return Result.Ok();
        }

        /// <summary>
        /// 更新参数规则
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<Result> UpdateParamRuleWithNaturalTextAsync(UpdateParamRuleTextRequest request, CancellationToken ct)
        {
            // 1. 获取现有规则
            var existingRule = await _pararmRuleRepository.GetByIdAsync(new ParamRuleId(request.Id), ct);

            if (existingRule == null)
                throw new Exception($"Param rule with id {request.Id} not found");

            var formula = await _formulaRepository.GetByIdAsync(existingRule.FormulaId!, ct);

            var (pattern, result) = _ruleTranslationService.ParseFromNaturalLanguageText(request.Text, formula, ct);

            // 3. 更新规则
            existingRule.Update(pattern, result, request.Priority, request.StopOnMatch);

            // 4. 持久化
            await _pararmRuleRepository.UpdateAsync(existingRule, ct);

            await _unitOfWork.SaveChangesAsync();

            // 5. 返回
            return Result.Ok();
        }

        /// <summary>
        /// 激活规则
        /// </summary>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result> ActiveParamRuleAsync(string id, CancellationToken ct)
        {
            var ruleId = new ParamRuleId(id);

            var rule = await _pararmRuleRepository.GetByIdAsync(ruleId, ct);

            if (rule.FormulaId == null)
                return Result.Fail("未查询到所属公式");
            if (rule.StructureId == null)
                return Result.Fail("未查询到所属参数结构");

            var formula = await _formulaRepository.GetByIdAsync(rule.FormulaId, ct);

            var paramStructure = await _paramStructureRepository.GetByIdAsync(rule.StructureId, ct);

           var isOk = _paramRuleValidateService.Validate(rule,formula,paramStructure);

            if (isOk.IsSuccess)
            {
                rule.Active();
            }

            await _pararmRuleRepository.UpdateAsync(rule, ct);

            await _unitOfWork.SaveChangesAsync();

            return Result.Ok();
        }

        /// <summary>
        /// 禁用规则
        /// </summary>
        public async Task<Result> DeactiveParamRuleAsync(string id, CancellationToken ct)
        {
            var ruleId = new ParamRuleId(id);
            var rule = await _pararmRuleRepository.GetByIdAsync(ruleId, ct);
            rule.Deactive();
            await _pararmRuleRepository.UpdateAsync(rule, ct);
            await _unitOfWork.SaveChangesAsync();
            return Result.Ok();
        }
    }
}
