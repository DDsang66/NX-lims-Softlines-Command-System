using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.TestItemContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using Microsoft.Extensions.Logging;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;

namespace NX_lims_Softlines_Command_System.src.Domain.Services
{
    /// <summary>
    /// 参数引擎调度器（领域服务）：
    /// - 根据传入的标准集合查询对应的标准族，收集公式与参数结构
    /// - 根据公式加载对应的规则集合
    /// 返回包含公式、结构与规则的聚合结果，供上层协调器/用例使用
    /// </summary>
    public class ParamEngineScheduler : IParamEngineScheduler, IScopedDependency
    {
        private readonly IStandardFamilyRepository _standardFamilyRepository;
        private readonly IFormulaRepository _formulaRepository;
        private readonly IParamStructureRepository _paramStructureRepository;
        private readonly IParamRuleRepository _paramRuleRepository;
        private readonly ILogger<ParamEngineScheduler> _logger;

        public ParamEngineScheduler(
            IStandardFamilyRepository standardFamilyRepository,
            IFormulaRepository formulaRepository,
            IParamStructureRepository paramStructureRepository,
            IParamRuleRepository paramRuleRepository,
            ILogger<ParamEngineScheduler> logger)
        {
            _standardFamilyRepository = standardFamilyRepository;
            _formulaRepository = formulaRepository;
            _paramStructureRepository = paramStructureRepository;
            _paramRuleRepository = paramRuleRepository;
            _logger = logger;
        }

        public async Task<ParamEngineScheduleResult> CollectForTestItemAsync(
            TestItemId testItemId,
            IEnumerable<StandardId> standardIds,
            CancellationToken cancellationToken)
        {
            if (standardIds == null) throw new ArgumentNullException(nameof(standardIds));

            var standardIdList = standardIds.ToList();
            if (!standardIdList.Any())
            {
                _logger.LogWarning("No standard ids provided for TestItem {TestItemId}", testItemId?.Value);
                return new ParamEngineScheduleResult();
            }

            // 1. 根据标准 ids 查询标准族集合
            var families = await _standardFamilyRepository.GetByStandardIdsAsync(standardIdList, cancellationToken);
            var familyList = families?.ToList() ?? new List<Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.StandardFamily>();

            // 2. 收集公式 id 与参数结构 id
            var formulaIdSet = new HashSet<FormulaId>();
            var paramStructureIdSet = new HashSet<ParamStructureId>();
            var familyIdList = new List<StandardFamilyId>();

            foreach (var fam in familyList)
            {
                if (fam == null) continue;

                // family.FormulaIds 可包含 nulls
                foreach (var fid in fam.FormulaIds.Where(x => x != null).Select(x => x!))
                    formulaIdSet.Add(fid);

                foreach (var pid in fam.ParamStructureIds.Where(x => x != null).Select(x => x!))
                    paramStructureIdSet.Add(pid);

                familyIdList.Add(fam.Id);
            }

            // 3. 加载公式
            var formulas = formulaIdSet.Any()
                ? (await _formulaRepository.GetByIdsAsync(formulaIdSet, cancellationToken)).ToList()
                : new List<Formula>();

            // 4. 加载参数结构（优先按公式查找，其次按 family）
            var structures = new List<ParamStructure>();
            if (formulaIdSet.Any())
            {
                var byFormula = await _paramStructureRepository.GetByFormulaIdsAsync(formulaIdSet.ToList(), cancellationToken);
                if (byFormula != null) structures.AddRange(byFormula);
            }

            if (familyIdList.Any())
            {
                var byFamily = await _paramStructureRepository.GetByFamilyIdsAsync(familyIdList, cancellationToken);
                if (byFamily != null) structures.AddRange(byFamily);
            }

            // 去重
            structures = structures.Distinct().ToList();

            // 5. 根据公式加载规则
            var rules = new List<ParamRule>();
            foreach (var formulaId in formulaIdSet)
            {
                try
                {
                    var rs = await _paramRuleRepository.GetByFormulaIdAsync(formulaId, cancellationToken);
                    if (rs != null) rules.AddRange(rs);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load rules for formula {FormulaId}", formulaId.Value);
                }
            }

            // 去重并按优先级排序
            var finalRules = rules
                .Where(r => r != null)
                .Distinct()
                .OrderBy(r => r.Priority)
                .ToList();

            return new ParamEngineScheduleResult
            {
                Formulas = formulas,
                ParamStructures = structures,
                Rules = finalRules
            };
        }
    }
}
