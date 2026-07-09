using DocumentFormat.OpenXml.Vml;
using Mapster;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;
using Formula = NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.Formula;

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

            if (formulaPo == null) return null;

            // 一行代码完成映射，包括 ConditionFields 的分割和值对象构造
            return formulaPo.Adapt<Formula>();
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

            return formulaPos.Adapt<List<Formula>>();
        }

        /// <summary>
        /// 通过参数名获取公式
        /// </summary>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Formula>> GetByParamName(string paramName,CancellationToken ct)
        {
            var formulaPos = await _context.BasicFormulas
                .AsNoTracking()
                .Where(s => s.ParamName == paramName)
                .ToListAsync(ct);

            return formulaPos.Adapt<List<Formula>>();
        }

        /// <summary>
        /// 添加公式
        /// </summary>
        /// <param name="formula"></param>
        /// <returns></returns>
        public async Task AddAsync(Formula formula, CancellationToken ct)
        {
            var formulaPo = formula.Adapt<BasicFormula>();

            await _context.AddAsync(formulaPo, ct);
        }

        /// <summary>
        /// 更新公式
        /// </summary>
        /// <param name="formula"></param>
        /// <returns></returns>
        public async Task UpdateAsync(Formula formula, CancellationToken ct)
        {
            var formulaPo = await _context.FindAsync<BasicFormula>(formula.Id.Value, ct);

            if (formulaPo == null)
                throw new Exception($"公式 {formula.Id.Value} 不存在");

            // 使用 Adapt 将领域模型的变更覆盖到已追踪的 PO 上
            formula.Adapt(formulaPo);
        }

        /// <summary>
        /// 批量更新公式
        /// </summary>
        /// <param name="fomulas"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task UpdateRangeAsync(IEnumerable<Formula> fomulas, CancellationToken ct)
        {
            var formulaList = fomulas.ToList();
            var ids = formulaList.Select(f => f.Id.Value).ToList();

            var existingPos = await _context.BasicFormulas
                .Where(f => ids.Contains(f.FormulaId))
                .ToListAsync(ct);

            var existingDict = existingPos.ToDictionary(f => f.FormulaId);

            foreach (var formula in formulaList)
            {
                if (!existingDict.TryGetValue(formula.Id.Value, out var po))
                    throw new Exception($"公式 {formula.Id.Value} 不存在");

                formula.Adapt(po);
            }
        }

        /// <summary>
        /// 删除公式
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task RemoveAsync(FormulaId id, CancellationToken ct)
        {
            var formulaPo = new BasicFormula { FormulaId = id.Value };
            _context.Attach(formulaPo);
            _context.Remove(formulaPo);
        }

    }
}
