using Mapster;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class ParamRuleRepository : IParamRuleRepository, IScopedDependency
    {
        private readonly dbContext _context;

        public ParamRuleRepository(dbContext Context) 
        {
            _context = Context;
        }

        /// <summary>
        /// 根据 id 获取参数规则
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<ParamRule> GetByIdAsync(ParamRuleId id, CancellationToken ct)
        {
            var paramRulePo = await _context.FindAsync<BasicParamRule>(id.Value, ct);

            if (paramRulePo == null) return null;

            return paramRulePo.Adapt<ParamRule>();
        }

        /// <summary>
        /// 获取参数规则集
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public Task<IEnumerable<ParamRule>> GetByIdsAsync(IEnumerable<ParamRuleId> ids,CancellationToken ct)
        {
            return null;
        }

        /// <summary>
        /// 根据公式 id 获取参数规则集
        /// </summary>
        /// <param name="formulaId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ParamRule>> GetByFormulaIdAsync(FormulaId formulaId)
        {
            return null;
        }

        /// <summary>
        /// 添加参数规则
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task AddAsync(ParamRule rule, CancellationToken ct) 
        {
            // 检查是否已经在内存中被追踪（可选）
            var existingEntity = await _context.FindAsync<BasicParamRule>(new object[] { rule.Id.Value }, ct);
            if (existingEntity != null)
            {
                // 如果已存在，根据业务逻辑抛出异常或直接返回
                throw new InvalidOperationException($"Rule with Id {rule.Id.Value} already exists.");
            }

            // 领域模型转数据库模型
            var rulePo = rule.Adapt<BasicParamRule>();

            // 加入 DbContext 追踪
            await _context.AddAsync(rulePo, ct);
        }

        public async Task UpdateAsync(ParamRule rule, CancellationToken ct) 
        {
            await Task.CompletedTask;
        }
    }
}
