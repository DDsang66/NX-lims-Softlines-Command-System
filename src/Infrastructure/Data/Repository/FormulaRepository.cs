using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class FormulaRepository : IFormulaRepository, IScopedDependency
    {
        /// <summary>
        /// 通过id获取公式
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async  Task<Formula>GetByIdAsync(FormulaId id, CancellationToken ct)
        {
            return null;
        }

        /// <summary>
        /// 通过id集合获取公式
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Formula>> GetByIdsAsync(IEnumerable<FormulaId> ids, CancellationToken ct)
        {
            return null;
        }

        /// <summary>
        /// 通过参数名获取公式
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public List<Formula> GetByParamName(string paramName)
        {
            return null;
        }

        /// <summary>
        /// 添加公式
        /// </summary>
        /// <param name="formula"></param>
        /// <returns></returns>
        public async Task AddAsync(Formula formula, CancellationToken ct)
        {

        }

        /// <summary>
        /// 更新公式
        /// </summary>
        /// <param name="formula"></param>
        /// <returns></returns>
        public async Task UpdateAsync(Formula formula, CancellationToken ct)
        {

        }

        /// <summary>
        /// 批量更新公式
        /// </summary>
        /// <param name="fomulas"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Formula>> UpdateRangeAsync(IEnumerable<Formula> fomulas, CancellationToken ct)
        {
            return null;
        }
    } 
}
