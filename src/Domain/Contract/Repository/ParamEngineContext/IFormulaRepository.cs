using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext
{
    public interface IFormulaRepository:IRepository<Formula,FormulaId,string>, IScopedDependency
    {
        /// <summary>
        /// 通过id获取公式
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<Formula> GetByIdAsync(FormulaId id,CancellationToken ct);

        /// <summary>
        /// 通过id集合获取公式
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        Task<IEnumerable<Formula>> GetByIdsAsync(IEnumerable<FormulaId> ids,CancellationToken ct);

        /// <summary>
        /// 获取所有公式
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<IEnumerable<Formula>> GetAllAsync(CancellationToken ct);

        /// <summary>
        /// 通过参数名获取公式
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        Task<IEnumerable<Formula>> GetByParamName(string paramName, CancellationToken ct);

        /// <summary>
        /// 添加公式
        /// </summary>
        /// <param name="formula"></param>
        /// <returns></returns>
        Task AddAsync(Formula formula, CancellationToken ct);

        /// <summary>
        /// 更新公式
        /// </summary>
        /// <param name="formula"></param>
        /// <returns></returns>
        Task UpdateAsync(Formula formula, CancellationToken ct);

        /// <summary>
        /// 批量更新公式
        /// </summary>
        /// <param name="fomulas"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task UpdateRangeAsync(IEnumerable<Formula> fomulas, CancellationToken ct);
    }
}
