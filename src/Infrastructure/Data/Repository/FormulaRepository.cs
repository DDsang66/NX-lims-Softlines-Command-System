using DocumentFormat.OpenXml.Vml;
using Mapster;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;
using System.Text.Json;
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
        /// 通过id获取公式（包含关联的 StandardFamily 和 ParamStructure）
        /// </summary>
        public async Task<Formula> GetByIdAsync(FormulaId id, CancellationToken ct)
        {
            // 1. 查询基础信息
            var formulaPo = await _context.BasicFormulas
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.FormulaId == id.Value, ct);

            if (formulaPo == null) return null;

            // 2. 查询关联的 StandardFamily
            var standardFamilyIds = await _context.FormulaStandardfamilies
                .Where(ff => ff.FormulaId == id.Value)
                .Select(ff => ff.IdStandardFamily)
                .ToListAsync(ct);

            // 3. 查询关联的 ParamStructure
            var paramStructureIds = await _context.ParamstructureFormulas
                .Where(pf => pf.FormulaId == id.Value)
                .Select(pf => pf.ParamStructureId)
                .ToListAsync(ct);

            // 4. 使用 Reconstitute 方法重建聚合根
            return Formula.Reconstitute(
                id,
                formulaPo.Name,
                formulaPo.ParamName,
                JsonSerializer.Deserialize<List<string>>(formulaPo.ConditionFields!) ?? new List<string>(),
                standardFamilyIds.Select(id => new StandardFamilyId(id)).ToList(),
                paramStructureIds.Select(id => new ParamStructureId(id)).ToList(),
                formulaPo.ExpressionTemplate!,
                formulaPo.Version ?? 0,
                formulaPo.IsActive,
                formulaPo.EffectiveDate??DateTime.UtcNow,
                formulaPo.Description
            );
        }

        /// <summary>
        /// 通过id集合获取公式
        /// </summary>
        public async Task<IEnumerable<Formula>> GetByIdsAsync(IEnumerable<FormulaId> ids, CancellationToken ct)
        {
            var idValues = ids.Select(id => id.Value).ToList();

            if (!idValues.Any())
                return Enumerable.Empty<Formula>();

            // 1. 查询基础信息
            var formulaPos = await _context.BasicFormulas
                .AsNoTracking()
                .Where(s => idValues.Contains(s.FormulaId))
                .ToListAsync(ct);

            // 2. 查询所有关联关系
            var allAssociations = await _context.FormulaStandardfamilies
                .Where(ff => idValues.Contains(ff.FormulaId))
                .ToListAsync(ct);

            var allParamAssociations = await _context.ParamstructureFormulas
                .Where(pf => idValues.Contains(pf.FormulaId))
                .ToListAsync(ct);

            // 3. 分组处理
            var result = new List<Formula>();
            foreach (var formulaPo in formulaPos)
            {
                var formulaId = formulaPo.FormulaId;

                // 获取当前公式的关联
                var standardFamilyIds = allAssociations
                    .Where(ff => ff.FormulaId == formulaId)
                    .Select(ff => ff.IdStandardFamily)
                    .ToList();

                var paramStructureIds = allParamAssociations
                    .Where(pf => pf.FormulaId == formulaId)
                    .Select(pf => pf.ParamStructureId)
                    .ToList();

                // 重建聚合根
                result.Add(Formula.Reconstitute(
                    new FormulaId(formulaId),
                    formulaPo.Name,
                    formulaPo.ParamName,
                    JsonSerializer.Deserialize<List<string>>(formulaPo.ConditionFields!) ?? new List<string>(),
                    standardFamilyIds.Select(id => new StandardFamilyId(id)).ToList(),
                    paramStructureIds.Select(id => new ParamStructureId(id)).ToList(),
                    formulaPo.ExpressionTemplate!,
                    formulaPo.Version ?? 0,
                    formulaPo.IsActive,
                    formulaPo.EffectiveDate ?? DateTime.UtcNow,
                    formulaPo.Description
                ));
            }

            return result;
        }

        /// <summary>
        /// 通过参数名获取公式
        /// </summary>
        public async Task<IEnumerable<Formula>> GetByParamName(string paramName, CancellationToken ct)
        {
            var formulaPos = await _context.BasicFormulas
                .AsNoTracking()
                .Where(s => s.ParamName == paramName)
                .ToListAsync(ct);

            // 类似 GetByIdsAsync 的处理方式
            var idValues = formulaPos.Select(f => f.FormulaId).ToList();

            var allAssociations = await _context.FormulaStandardfamilies
                .Where(ff => idValues.Contains(ff.FormulaId))
                .ToListAsync(ct);

            var allParamAssociations = await _context.ParamstructureFormulas
                .Where(pf => idValues.Contains(pf.FormulaId))
                .ToListAsync(ct);

            var result = new List<Formula>();
            foreach (var formulaPo in formulaPos)
            {
                var formulaId = formulaPo.FormulaId;

                var standardFamilyIds = allAssociations
                    .Where(ff => ff.FormulaId == formulaId)
                    .Select(ff => ff.IdStandardFamily)
                    .ToList();

                var paramStructureIds = allParamAssociations
                    .Where(pf => pf.FormulaId == formulaId)
                    .Select(pf => pf.ParamStructureId)
                    .ToList();

                result.Add(Formula.Reconstitute(
                    new FormulaId(formulaId),
                    formulaPo.Name,
                    formulaPo.ParamName,
                    JsonSerializer.Deserialize<List<string>>(formulaPo.ConditionFields!) ?? new List<string>(),
                    standardFamilyIds.Select(id => new StandardFamilyId(id)).ToList(),
                    paramStructureIds.Select(id => new ParamStructureId(id)).ToList(),
                    formulaPo.ExpressionTemplate!,
                    formulaPo.Version ?? 0,
                    formulaPo.IsActive,
                    formulaPo.EffectiveDate ?? DateTime.UtcNow,
                    formulaPo.Description
                ));
            }

            return result;
        }

        /// <summary>
        /// 添加公式
        /// </summary>
        public async Task AddAsync(Formula formula, CancellationToken ct)
        {
            // 1. 添加基础信息
            var formulaPo = formula.Adapt<BasicFormula>();

            await _context.AddAsync(formulaPo, ct);

            // 2. 后续通过事件处理关联关系
            //foreach (var familyId in formula.StandardFamilyIds.Where(id => id != null))
            //{
            //    if (!await _context.FormulaStandardfamilies
            //        .AnyAsync(af =>
            //            af.FormulaId == formulaPo.FormulaId &&
            //            af.IdStandardFamily == familyId!.Value,
            //            ct))
            //    {
            //        await _context.AddAsync(new FormulaStandardfamily
            //        {
            //            FormulaId = formulaPo.FormulaId,
            //            IdStandardFamily = familyId!.Value
            //        }, ct);
            //    }
            //}

            //foreach (var paramId in formula.ParamSturctureIds.Where(id => id != null))
            //{
            //    if (!await _context.ParamstructureFormulas
            //        .AnyAsync(af =>
            //            af.FormulaId == formulaPo.FormulaId &&
            //            af.ParamStructureId == paramId.Value,
            //            ct))
            //    {
            //        await _context.AddAsync(new ParamstructureFormula
            //        {
            //            FormulaId = formulaPo.FormulaId,
            //            ParamStructureId = paramId!.Value
            //        }, ct);
            //    }
            //}
        }

        /// <summary>
        /// 更新公式
        /// </summary>
        public async Task UpdateAsync(Formula formula, CancellationToken ct)
        {
            // 1. 获取现有实体
            var existingPo = await _context.BasicFormulas
                .Include(f => f.FormulaStandardfamilies)
                .Include(f => f.ParamstructureFormulas)
                .FirstOrDefaultAsync(f => f.FormulaId == formula.Id.Value, ct);

            if (existingPo == null)
                throw new Exception($"公式 {formula.Id.Value} 不存在");

            // 2. 更新基础信息
            formula.Adapt(existingPo);

            //// 3. 处理 StandardFamily 关联
            //var targetFamilyIds = formula.StandardFamilyIds.Select(id => id!.Value).ToList();
            //var currentFamilyIds = existingPo.FormulaStandardfamilies.Select(f => f.IdStandardFamily).ToList();

            //// 添加新关联
            //foreach (var newId in targetFamilyIds.Except(currentFamilyIds))
            //{
            //    existingPo.FormulaStandardfamilies.Add(new FormulaStandardfamily
            //    {
            //        FormulaId = existingPo.FormulaId,
            //        IdStandardFamily = newId
            //    });
            //}

            //// 4. 处理 ParamStructure 关联
            //var targetParamIds = formula.ParamSturctureIds.Select(id => id!.Value).ToList();
            //var currentParamIds = existingPo.ParamstructureFormulas.Select(p => p.ParamStructureId).ToList();

            //// 添加新关联
            //foreach (var newId in targetParamIds.Except(currentParamIds))
            //{
            //    existingPo.ParamstructureFormulas.Add(new ParamstructureFormula
            //    {
            //        FormulaId = existingPo.FormulaId,
            //        ParamStructureId = newId
            //    });
            //}
        }

        /// <summary>
        /// 批量更新公式
        /// </summary>
        public async Task UpdateRangeAsync(IEnumerable<Formula> formulas, CancellationToken ct)
        {
            var formulaList = formulas.ToList();
            var ids = formulaList.Select(f => f.Id.Value).ToList();

            // 1. 获取所有现有实体及其关联
            var existingPos = await _context.BasicFormulas
                .Where(f => ids.Contains(f.FormulaId))
                .Include(f => f.FormulaStandardfamilies)
                .Include(f => f.ParamstructureFormulas)
                .ToListAsync(ct);

            var existingDict = existingPos.ToDictionary(f => f.FormulaId);

            // 2. 更新每个公式
            foreach (var formula in formulaList)
            {
                if (!existingDict.TryGetValue(formula.Id.Value, out var po))
                    throw new Exception($"公式 {formula.Id.Value} 不存在");

                // 更新基础信息
                formula.Adapt(po);

                //// 更新关联关系（复用 UpdateAsync 中的逻辑）
                //var targetFamilyIds = formula.StandardFamilyIds.Select(id => id!.Value).ToList();
                //var currentFamilyIds = po.FormulaStandardfamilies.Select(f => f.IdStandardFamily).ToList();

                //foreach (var newId in targetFamilyIds.Except(currentFamilyIds))
                //{
                //    po.FormulaStandardfamilies.Add(new FormulaStandardfamily
                //    {
                //        FormulaId = po.FormulaId,
                //        IdStandardFamily = newId
                //    });
                //}

                //var targetParamIds = formula.ParamSturctureIds.Select(id => id!.Value).ToList();
                //var currentParamIds = po.ParamstructureFormulas.Select(p => p.ParamStructureId).ToList();

                //foreach (var newId in targetParamIds.Except(currentParamIds))
                //{
                //    po.ParamstructureFormulas.Add(new ParamstructureFormula
                //    {
                //        FormulaId = po.FormulaId,
                //        ParamStructureId = newId
                //    });
                //}
            }
        }

        /// <summary>
        /// 删除公式
        /// </summary>
        public async Task RemoveAsync(FormulaId id, CancellationToken ct)
        {
            // 1. 获取实体及其关联
            var formulaPo = await _context.BasicFormulas
                .Include(f => f.FormulaStandardfamilies)
                .Include(f => f.ParamstructureFormulas)
                .FirstOrDefaultAsync(f => f.FormulaId == id.Value, ct);

            if (formulaPo == null)
                return;

            //// 2. 删除所有关联（级联删除或手动删除）
            //_context.FormulaStandardfamilies.RemoveRange(formulaPo.FormulaStandardfamilies);
            //_context.ParamstructureFormulas.RemoveRange(formulaPo.ParamstructureFormulas);

            // 3. 删除主记录
            _context.BasicFormulas.Remove(formulaPo);
        }
    }
}
