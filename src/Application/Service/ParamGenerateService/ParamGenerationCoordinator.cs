using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.CheckListContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ConditionPoolContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Service.Engine.Condition;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using System.Threading.Tasks;

namespace NX_lims_Softlines_Command_System.src.Application.Service.ParamGenerateService
{
    /// <summary>
    /// 应用层协调器：负责跨聚合加载、调用富化器/引擎/补偿服务、并返回最终 ParamSet
    /// - 不直接持久化（调用方可在事务边界内保存）
    /// </summary>
    public class ParamGenerationCoordinator:IScopedDependency
    {
        private readonly IParamStructureRepository _structureRepo;
        private readonly IFormulaRepository _formulaRepo;
        private readonly IParamRuleRepository _ruleRepo;
        private readonly IParamGenerationEngine _engine;
        private readonly IParamCompensationService _compensation;
        private readonly IConditionPoolValidateService _conditionPoolValidateService;

        public ParamGenerationCoordinator(
            IParamStructureRepository structureRepo,
            IFormulaRepository formulaRepo,
            IParamRuleRepository ruleRepo,
            IParamGenerationEngine engine,
            IParamCompensationService compensation,
            IConditionPoolValidateService conditionPoolValidateService)
        {
            _structureRepo = structureRepo;
            _conditionPoolValidateService = conditionPoolValidateService;
            _formulaRepo = formulaRepo;
            _ruleRepo = ruleRepo;
            _engine = engine;
            _compensation = compensation;
        }

        /// <summary>
        /// 主流程：
        /// 1. 加载 ParamStructure
        /// 2. 使用 ParamStructure/Formula 验证 ConditionPool,对其中的差异值进行条件富化
        /// 3. 加载规则并调用引擎生成
        /// 4. 调用补偿服务得到最终 ParamSet
        /// </summary>
        public async Task<Result<ParamSet>> GenerateForStructure(string formulaId,string structureId, ConditionPool pool,CancellationToken ct)
        {
            // 1. 加载结构
            var structure = await _structureRepo.GetByIdAsync(new ParamStructureId(structureId),ct);
            if (structure == null) return Result<ParamSet>.Fail("ParamStructure not found");

            // 2. 前置验证（结构层面）
            var v1 = await  _conditionPoolValidateService.EnsureConditionPoolConformance(structure, pool);
            if (v1.IsFailure) return Result<ParamSet>.Fail(v1.Error);

            //ConditionPool调用ConditionEnricher进行富化，确保所有条件字段都被填充
            //交由Formula进行语义检查，确保所有必需的条件字段都存在

            // 3. 加载 Formula 并做二级条件池语义检查
            var formula = await _formulaRepo.GetByIdAsync(new FormulaId(formulaId), ct);
            if (formula == null) return Result<ParamSet>.Fail("Formula not found");

            var v2 = await _conditionPoolValidateService.EnsureConditionPoolWithFormula(formula, pool);
            if (v2.IsFailure) return Result<ParamSet>.Fail($"Missing required conditions: {string.Join(',', v2)}");

            // 4. 加载规则
            var rules = await _ruleRepo.GetByIdsAsync(structure.ApplicableRuleIds, ct);

            // 5. 引擎生成
            var generated = _engine.Generate(pool, rules);

            // 6. 补偿
            var final = _compensation.ConformToStructure(generated, structure);

            return Result<ParamSet>.Ok(final);
        }
    }
}
