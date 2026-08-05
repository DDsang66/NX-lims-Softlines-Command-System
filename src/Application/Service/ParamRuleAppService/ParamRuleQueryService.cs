using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamFormulaContext;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository;

namespace NX_lims_Softlines_Command_System.src.Application.Service.ParamRuleAppService
{
    public class ParamRuleQueryService:IParamRuleQueryService,IScopedDependency
    {
        private readonly IParamRuleRepository _ruleRepo;
        private readonly IParamStructureRepository _structureRepo;
        private readonly IStandardFamilyRepository _familyRepo;

        public ParamRuleQueryService(
            IParamRuleRepository ruleRepo, 
            IParamStructureRepository structureRepo,
            IStandardFamilyRepository familyRepo) 
        {
            _ruleRepo = ruleRepo;
            _structureRepo = structureRepo;
            _familyRepo = familyRepo;
        }

        /// <summary>
        /// 查询规则
        /// </summary>
        /// <param name="standardId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<Dictionary<ParamStructureId, List<ParamRule>>> GetRulesByStandardAsync(
            StandardId standardId,
            CancellationToken ct)
        {
            var family = await _familyRepo.GetByStandardIdAsync(standardId, ct);
            if (family == null) throw new Exception("未找到标准所属的 Family");

            var structures = await _structureRepo.GetByFamilyIdAsync(family.Id, ct);

            var result = new Dictionary<ParamStructureId, List<ParamRule>>();
            foreach (var structure in structures)
            {
                var rules = await _ruleRepo.GetByIdsAsync(structure.ApplicableRuleIds, ct);
                result[structure.Id] = rules.ToList();
            }

            return result;
        }

        /// <summary>
        /// 获取参数规则
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<Result<ParamRuleResponseDto>> GetByIdAsync(string id, CancellationToken ct)
        {
            var rule = await _ruleRepo.GetByIdAsync(new ParamRuleId(id), ct);

            if (rule == null)
                throw new Exception($"Param rule with id {id} not found");

            var ruleDto = rule.Adapt<ParamRuleResponseDto>();

            return Result<ParamRuleResponseDto>.Ok(ruleDto);
        }

        /// <summary>
        /// 获取参数规则
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<Result<List<ParamRuleResponseDto>>> GetByIdsAsync(IEnumerable<string> ids, CancellationToken ct)
        {
            if (ids == null || !ids.Any())
            {
                return Result<List<ParamRuleResponseDto>>.Ok(new List<ParamRuleResponseDto>());
            }
            // 将字符串ID转换为FormulaId对象
            var ruleIds = ids.Select(id => new ParamRuleId(id)).ToList();

            var rules = await  _ruleRepo.GetByIdsAsync(ruleIds, ct);

            if (rules == null)
                throw new Exception($"Param rule with id {ids} not found");

            var ruleDtoList = rules.Adapt<List<ParamRuleResponseDto>>();

            return Result<List<ParamRuleResponseDto>>.Ok(ruleDtoList);
        }


        /// <summary>
        /// 获取所有参数规则
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<Result<List<ParamRuleResponseDto>>> GetAllRulesAsync(CancellationToken ct)
        {
            var rules = await _ruleRepo.GetAllRulesAsync(ct);

            var ruleDtos = rules.Adapt<List<ParamRuleResponseDto>>();

            return Result<List<ParamRuleResponseDto>>.Ok(ruleDtos);
        }

        /// <summary>
        /// 根据公式id获取参数规则
        /// </summary>
        /// <param name="formulaId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Result<List<ParamRuleResponseDto>>> GetRulesByFormulaIdAsync(string formulaId, CancellationToken ct) 
        {
            var rules = await _ruleRepo.GetByFormulaIdAsync(new FormulaId(formulaId), ct);

            var ruleDtos = rules.Adapt<List<ParamRuleResponseDto>>();

            return Result<List<ParamRuleResponseDto>>.Ok(ruleDtos);
        }
    }
}
