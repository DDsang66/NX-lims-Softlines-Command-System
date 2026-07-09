using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext
{
    public interface IParamRuleRepository: IRepository<ParamRule>, IScopedDependency
    {
        /// <summary>
        /// 根据公式查询所有规则
        /// </summary>
        /// <param name="formulaId"></param>
        /// <returns></returns>
       Task<IEnumerable<ParamRule>> GetByFormulaIdAsync(FormulaId formulaId);

        /// <summary>
        /// 根据id查询规则
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<ParamRule> GetByIdAsync(ParamRuleId id,CancellationToken ct);

        /// <summary>
        /// 查询规则集
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<ParamRule>> GetByIdsAsync(IEnumerable<ParamRuleId> ids, CancellationToken ct);

        /// <summary>
        /// 添加规则
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task AddAsync(ParamRule rule,CancellationToken ct);

        /// <summary>
        /// 更新规则
        /// </summary>
        /// <param name="rule"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UpdateAsync(ParamRule rule,CancellationToken ct);
    }
}
