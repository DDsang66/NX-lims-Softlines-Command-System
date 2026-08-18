using DocumentFormat.OpenXml.Office2010.Excel;
using Mapster;
using Microsoft.EntityFrameworkCore;
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
        public async Task<IEnumerable<ParamRule>> GetByIdsAsync(IEnumerable<ParamRuleId> ids,CancellationToken ct)
        {
            // 1. 防御性校验：如果为空，直接返回空集合，避免生成无效的 SQL (WHERE IN ())
            if (ids == null || !ids.Any())
            {
                return Enumerable.Empty<ParamRule>();
            }

            // 2. 提取实际的主键值。因为 ParamRuleId 是自定义结构，需要取出它的 Value
            // 尽早 ToList()，避免 IEnumerable 延迟执行带来的多次枚举问题
            var idValues = ids.Select(id => id.Value).ToList();

            // 3. 一次性从数据库批量查询，EF Core 会自动翻译为 WHERE Value IN (@p0, @p1...)
            var paramRulePos = await _context.Set<BasicParamRule>()
                .Where(po => idValues.Contains(po.RuleId)) // 注意：这里假设 BasicParamRule 的主键属性名叫 Value
                .ToListAsync(ct);

            // 4. 批量映射 (Mapster/AutoMapper 等)
            return paramRulePos.Adapt<List<ParamRule>>();
        }

        /// <summary>
        /// 获取所有参数规则
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ParamRule>> GetAllRulesAsync(CancellationToken ct) 
        {
            var paramRulePos = await _context.Set<BasicParamRule>()
                .ToListAsync(ct);

            return paramRulePos.Adapt<List<ParamRule>>();
        }

        /// <summary>
        /// 根据公式 id 获取参数规则集
        /// </summary>
        /// <param name="formulaId"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ParamRule>> GetByFormulaIdAsync(FormulaId formulaId, CancellationToken ct)
        {
            // 提取实际的主键值。因为 ParamRuleId 是自定义结构，需要取出它的 Value
            // 尽早 ToList()，避免 IEnumerable 延迟执行带来的多次枚举问题

            // 3. 一次性从数据库批量查询，EF Core 会自动翻译为 WHERE Value IN (@p0, @p1...)
            var paramRulePos = await _context.Set<BasicParamRule>()
                .Where(po => po.FormulaId.Contains(formulaId)) // 注意：这里假设 BasicParamRule 的主键属性名叫 Value
                .ToListAsync(ct);

            // 4. 批量映射 (Mapster/AutoMapper 等)
            return paramRulePos.Adapt<List<ParamRule>>();
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

        /// <summary>
        /// 更新规则
        /// </summary>
        public async Task UpdateAsync(ParamRule rule, CancellationToken ct)
        {
            // 1. 根据主键查找被 EF Core 追踪的持久化对象
            var po = await _context.FindAsync<BasicParamRule>(rule.Id.Value, ct);

            // 2. 防御性校验：如果不存在，根据业务逻辑抛出异常
            if (po == null)
            {
                throw new KeyNotFoundException($"未找到Id为 {rule.Id.Value} 的参数规则，无法更新。");
            }

            // 3. 将领域模型 rule 的属性映射更新到持久化对象 po 上
            // Mapster 的 Adapt 方法会将源对象的属性值覆盖到目标对象上
            rule.Adapt(po);

            _context.Update(po);
            // 因为 po 是通过 FindAsync 查出来的，已经被 EF Core 的变更追踪器追踪。
            // 当我们修改了 po 的属性后，追踪器会自动将其状态标记为 Modified，
            // 在调用 SaveChangesAsync 时会自动生成 UPDATE SQL 语句。
        }

    }
}
