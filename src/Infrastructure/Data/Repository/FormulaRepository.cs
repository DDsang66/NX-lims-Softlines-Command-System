using Mapster;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class FormulaRepository : IFormulaRepository, IScopedDependency
    {
        private readonly dbContext _context;

        public FormulaRepository(dbContext context)
        {
            _context = context;
        }
        /// <summary>
        /// 通过id获取公式
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async  Task<Formula>GetByIdAsync(FormulaId id, CancellationToken ct)
        {
            var formulaPo = await _context.FindAsync<BasicFormula>(id.Value, ct);

            var formula = Formula.Reconstitute(
                new FormulaId(formulaPo.FormulaId),
                formulaPo.Name,
                formulaPo.ParamName,
                formulaPo.ConditionFields?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>(),
                new StandardFamilyId(formulaPo.StandardFamilyCodeId),
                formulaPo.ExpressionTemplate,
                formulaPo.Description,
                formulaPo.Version ?? 0,
                formulaPo.IsActive,
                formulaPo.EffectiveDate
                );

            return formula;
        }

        /// <summary>
        /// 通过id集合获取公式
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Formula>> GetByIdsAsync(IEnumerable<FormulaId> ids, CancellationToken ct)
        {
            var idValues = ids.Select(id => id.Value).ToList();

            if (!idValues.Any())
                return Enumerable.Empty<Formula>();

            var formulaPos = await _context.BasicFormulas
                .AsNoTracking()
                .Where(s => idValues.Contains(s.FormulaId))
                .ToListAsync(ct);

            var formulas = formulaPos.Adapt<List<Formula>>();

            return formulas;
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
            await _context.AddAsync(formula, ct);

            await Task.CompletedTask;
        }

        /// <summary>
        /// 更新公式
        /// </summary>
        /// <param name="formula"></param>
        /// <returns></returns>
        public async Task UpdateAsync(Formula formula, CancellationToken ct)
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// 批量更新公式
        /// </summary>
        /// <param name="fomulas"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Formula>> UpdateRangeAsync(IEnumerable<Formula> fomulas, CancellationToken ct)
        {
            await Task.CompletedTask;

            return fomulas;
        }
    } 
}
