using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Vml;
using Mapster;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.BuyerContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;
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
            var paramStructureIds = await _context.BasicParamStructures
                .Where(pf => pf.FormulaId == id.Value)
                .Select(pf => pf.ParamStructureId)
                .ToListAsync(ct);

            // 4. 使用 Reconstitute 方法重建聚合根
            // 5. 查询关联的 Buyer（若有）
            var buyerAssociations = await _context.FormulaBuyers
                .AsNoTracking()
                .Where(fb => fb.FormulaId == id.Value)
                .Select(fb => fb.BuyerId)
                .ToListAsync(ct);

            var buyerIds = buyerAssociations.Select(b => new BuyerId(b)).ToList();

            // 6. 映射 EngineLayer
            var engineLayer = EngineLayer.Standard;
            if (formulaPo.EngineLayer.HasValue)
            {
                try
                {
                    engineLayer = (EngineLayer)formulaPo.EngineLayer.Value;
                }
                catch { engineLayer = EngineLayer.Standard; }
            }

            // 7. 使用 Reconstitute 方法重建聚合根
            return Formula.Reconstitute(
                id,
                formulaPo.Name,
                formulaPo.ParamName,
                JsonSerializer.Deserialize<List<string>>(formulaPo.ConditionFields!) ?? new List<string>(),
                standardFamilyIds.Select(sId => new StandardFamilyId(sId)).ToList(),
                paramStructureIds.Select(pId => new ParamStructureId(pId)).ToList(),
                buyerIds,
                formulaPo.ExpressionTemplate ?? string.Empty,
                formulaPo.Version ?? 0,
                formulaPo.IsActive,
                formulaPo.EffectiveDate ?? DateTime.UtcNow,
                engineLayer,
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

            // 2. 批量查询所有 StandardFamily 关联关系，避免 N+1 查询
            var allFamilyAssociations = await _context.FormulaStandardfamilies
                .Where(ff => idValues.Contains(ff.FormulaId))
                .ToListAsync(ct);

            // 3. 批量查询所有 ParamStructure 关联关系，避免 N+1 查询
            var allParamAssociations = await _context.BasicParamStructures
                .Where(pf => idValues.Contains(pf.FormulaId))
                .ToListAsync(ct);

            // 批量查询所有 FormulaBuyer 关联
            var allBuyerAssociations = await _context.FormulaBuyers
                .AsNoTracking()
                .Where(fb => idValues.Contains(fb.FormulaId))
                .ToListAsync(ct);

            // 4. 分组处理并在内存中组装
            var result = new List<Formula>();
            foreach (var formulaPo in formulaPos)
            {
                var formulaId = formulaPo.FormulaId;

                var standardFamilyIds = allFamilyAssociations
                    .Where(ff => ff.FormulaId == formulaId)
                    .Select(ff => ff.IdStandardFamily)
                    .ToList();

                var paramStructureIds = allParamAssociations
                    .Where(pf => pf.FormulaId == formulaId)
                    .Select(pf => pf.ParamStructureId)
                    .ToList();

                // load buyers for this formula
                var buyerIds = allBuyerAssociations
                    .Where(b => b.FormulaId == formulaId)
                    .Select(b => b.BuyerId)
                    .ToList();

                // map engine layer
                var engineLayer = EngineLayer.Standard;
                if (formulaPo.EngineLayer.HasValue)
                {
                    try { engineLayer = (EngineLayer)formulaPo.EngineLayer.Value; } catch { engineLayer = EngineLayer.Standard; }
                }

                result.Add(Formula.Reconstitute(
                    new FormulaId(formulaId),
                    formulaPo.Name,
                    formulaPo.ParamName,
                    JsonSerializer.Deserialize<List<string>>(formulaPo.ConditionFields!) ?? new List<string>(),
                    standardFamilyIds.Select(sId => new StandardFamilyId(sId)).ToList(),
                    paramStructureIds.Select(pId => new ParamStructureId(pId)).ToList(),
                    buyerIds.Select(b => new BuyerId(b)).ToList(),
                    formulaPo.ExpressionTemplate ?? string.Empty,
                    formulaPo.Version ?? 0,
                    formulaPo.IsActive,
                    formulaPo.EffectiveDate ?? DateTime.UtcNow,
                    engineLayer,
                    formulaPo.Description
                ));
            }

            return result;
        }

        /// <summary>
        /// 获取所有公式（包含关联的 StandardFamily 和 ParamStructure）
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Formula>> GetAllAsync(CancellationToken ct)
        {
            // 1. 查询基础信息
            var formulaPos = await _context.BasicFormulas
                .AsNoTracking()
                .ToListAsync(ct);

            if (!formulaPos.Any()) return Enumerable.Empty<Formula>();

            var idValues = formulaPos.Select(f => f.FormulaId).ToList();

            // 2. 批量查询所有关联关系，避免 N+1 查询
            var allFamilyAssociations = await _context.FormulaStandardfamilies
                .Where(ff => idValues.Contains(ff.FormulaId))
                .ToListAsync(ct);

            var allParamAssociations = await _context.BasicParamStructures
                .Where(pf => idValues.Contains(pf.FormulaId))
                .ToListAsync(ct);

            var allBuyerAssociations = await _context.FormulaBuyers
                .AsNoTracking()
                .Where(fb => idValues.Contains(fb.FormulaId))
                .ToListAsync(ct);

            // 3. 分组处理
            var result = new List<Formula>();
            foreach (var formulaPo in formulaPos)
            {
                var formulaId = formulaPo.FormulaId;

                var standardFamilyIds = allFamilyAssociations
                    .Where(ff => ff.FormulaId == formulaId)
                    .Select(ff => ff.IdStandardFamily)
                    .ToList();

                var paramStructureIds = allParamAssociations
                    .Where(pf => pf.FormulaId == formulaId)
                    .Select(pf => pf.ParamStructureId)
                    .ToList();

                // load buyers for this formula
                var buyers = allBuyerAssociations
                    .Where(b => b.FormulaId == formulaId)
                    .Select(b => b.BuyerId)
                    .ToList();

                var buyerIds = buyers.Select(b => new BuyerId(b)).ToList();

                // map engine layer
                var engineLayer = EngineLayer.Standard;
                if (formulaPo.EngineLayer.HasValue)
                {
                    try { engineLayer = (EngineLayer)formulaPo.EngineLayer.Value; } catch { engineLayer = EngineLayer.Standard; }
                }

                result.Add(Formula.Reconstitute(
                    new FormulaId(formulaId),
                    formulaPo.Name,
                    formulaPo.ParamName,
                    JsonSerializer.Deserialize<List<string>>(formulaPo.ConditionFields!) ?? new List<string>(),
                    standardFamilyIds.Select(sId => new StandardFamilyId(sId)).ToList(),
                    paramStructureIds.Select(pId => new ParamStructureId(pId)).ToList(),
                    buyerIds,
                    formulaPo.ExpressionTemplate ?? string.Empty,
                    formulaPo.Version ?? 0,
                    formulaPo.IsActive,
                    formulaPo.EffectiveDate ?? DateTime.UtcNow,
                    engineLayer,
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

            if (!formulaPos.Any()) return Enumerable.Empty<Formula>();

            var idValues = formulaPos.Select(f => f.FormulaId).ToList();

            var allFamilyAssociations = await _context.FormulaStandardfamilies
                .Where(ff => idValues.Contains(ff.FormulaId))
                .ToListAsync(ct);

            var allParamAssociations = await _context.BasicParamStructures
                .Where(pf => idValues.Contains(pf.FormulaId))
                .ToListAsync(ct);

            var allBuyerAssociations = await _context.FormulaBuyers
                .AsNoTracking()
                .Where(fb => idValues.Contains(fb.FormulaId))
                .ToListAsync(ct); 

            var result = new List<Formula>();
            foreach (var formulaPo in formulaPos)
            {
                var formulaId = formulaPo.FormulaId;

                var standardFamilyIds = allFamilyAssociations
                    .Where(ff => ff.FormulaId == formulaId)
                    .Select(ff => ff.IdStandardFamily)
                    .ToList();

                var paramStructureIds = allParamAssociations
                    .Where(pf => pf.FormulaId == formulaId)
                    .Select(pf => pf.ParamStructureId)
                    .ToList();

                var buyerIds = allBuyerAssociations
                    .Where(b => b.FormulaId == formulaId)
                    .Select(b => b.BuyerId)
                    .ToList();

                result.Add(Formula.Reconstitute(
                    new FormulaId(formulaId),
                    formulaPo.Name,
                    formulaPo.ParamName,
                    JsonSerializer.Deserialize<List<string>>(formulaPo.ConditionFields!) ?? new List<string>(),
                    standardFamilyIds.Select(sId => new StandardFamilyId(sId)).ToList(),
                    paramStructureIds.Select(pId => new ParamStructureId(pId)).ToList(),
                    buyerIds.Select(b => new BuyerId(b)).ToList(),
                    formulaPo.ExpressionTemplate!,
                    formulaPo.Version ?? 0,
                    formulaPo.IsActive,
                    formulaPo.EffectiveDate ?? DateTime.UtcNow,
                    (EngineLayer)(formulaPo.EngineLayer ?? 0),
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
            // serialize condition fields and map engine layer
            formulaPo.ConditionFields = JsonSerializer.Serialize(formula.ConditionFields);
            formulaPo.EngineLayer = (byte)formula.EngineLayer;

            await _context.AddAsync(formulaPo, ct);

            // 2. 后续通过事件处理关联关系
            foreach (var familyId in formula.StandardFamilyIds.Where(id => id != null))
            {
                if (!await _context.FormulaStandardfamilies
                    .AnyAsync(af =>
                        af.FormulaId == formulaPo.FormulaId &&
                        af.IdStandardFamily == familyId!.Value,
                        ct))
                {
                    await _context.AddAsync(new FormulaStandardfamily
                    {
                        FormulaId = formulaPo.FormulaId,
                        IdStandardFamily = familyId!.Value
                    }, ct);
                }

            // 添加买家关联
            if (formula.BuyerIds != null)
            {
                foreach (var buyerId in formula.BuyerIds.Where(b => b != null))
                {
                    if (!await _context.FormulaBuyers.AnyAsync(fb => fb.FormulaId == formulaPo.FormulaId && fb.BuyerId == buyerId!.Value, ct))
                    {
                        await _context.AddAsync(new FormulaBuyer
                        {
                            FormulaId = formulaPo.FormulaId,
                            BuyerId = buyerId!.Value
                        }, ct);
                    }
                }
            }
            }

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
            // 1. 获取现有实体及其关联
            var existingPo = await _context.BasicFormulas
                .Include(f => f.FormulaStandardfamilies)
                .FirstOrDefaultAsync(f => f.FormulaId == formula.Id.Value, ct);

            if (existingPo == null)
                throw new Exception($"公式 {formula.Id.Value} 不存在");

            // 2. 更新基础信息
            formula.Adapt(existingPo);
            existingPo.ConditionFields = JsonSerializer.Serialize(formula.ConditionFields);

            // 3. 处理 StandardFamily 关联 (差集计算：添加新增的，移除解除的)
            var targetFamilyIds = formula.StandardFamilyIds.Select(id => id!.Value).ToList();
            var currentFamilyIds = existingPo.FormulaStandardfamilies.Select(f => f.IdStandardFamily).ToList();

            // 添加新关联
            foreach (var newId in targetFamilyIds.Except(currentFamilyIds))
            {
                existingPo.FormulaStandardfamilies.Add(new FormulaStandardfamily
                {
                    FormulaId = existingPo.FormulaId,
                    IdStandardFamily = newId
                });
            }

            // 移除旧关联
            var toRemoveFamilies = existingPo.FormulaStandardfamilies
                .Where(f => !targetFamilyIds.Contains(f.IdStandardFamily))
                .ToList();
            _context.FormulaStandardfamilies.RemoveRange(toRemoveFamilies);

            // 4. 处理 ParamStructure 关联 (如果有独立中间表，逻辑同上)
            // ... (根据实际表结构补充)

            // 5. 处理买家关联（差集计算）
            var targetBuyerIds = formula.BuyerIds?.Select(b => b!.Value).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();
            var currentBuyerAssociations = await _context.FormulaBuyers
                .Where(fb => fb.FormulaId == existingPo.FormulaId)
                .ToListAsync(ct);

            var currentBuyerIds = currentBuyerAssociations.Select(b => b.BuyerId).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // 添加新关联
            foreach (var newBuyer in targetBuyerIds.Except(currentBuyerIds))
            {
                await _context.AddAsync(new FormulaBuyer
                {
                    FormulaId = existingPo.FormulaId,
                    BuyerId = newBuyer
                }, ct);
            }

            // 移除旧关联
            var toRemoveBuyers = currentBuyerAssociations
                .Where(b => !targetBuyerIds.Contains(b.BuyerId))
                .ToList();
            if (toRemoveBuyers.Any()) _context.FormulaBuyers.RemoveRange(toRemoveBuyers);
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
                .ToListAsync(ct);

            var existingDict = existingPos.ToDictionary(f => f.FormulaId);

            // 2. 更新每个公式
            foreach (var formula in formulaList)
            {
                if (!existingDict.TryGetValue(formula.Id.Value, out var po))
                    throw new Exception($"公式 {formula.Id.Value} 不存在");

                // 更新基础信息
                formula.Adapt(po);
                po.ConditionFields = JsonSerializer.Serialize(formula.ConditionFields);

                // 更新 StandardFamily 关联关系
                var targetFamilyIds = formula.StandardFamilyIds.Select(id => id!.Value).ToList();
                var currentFamilyIds = po.FormulaStandardfamilies.Select(f => f.IdStandardFamily).ToList();

                foreach (var newId in targetFamilyIds.Except(currentFamilyIds))
                {
                    po.FormulaStandardfamilies.Add(new FormulaStandardfamily
                    {
                        FormulaId = po.FormulaId,
                        IdStandardFamily = newId
                    });
                }

                var toRemoveFamilies = po.FormulaStandardfamilies
                    .Where(f => !targetFamilyIds.Contains(f.IdStandardFamily))
                    .ToList();
                _context.FormulaStandardfamilies.RemoveRange(toRemoveFamilies);

                // update buyers for this formula
                var targetBuyerIds = formula.BuyerIds?.Select(b => b!.Value).ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new HashSet<string>();
                var currentBuyerAssociations = await _context.FormulaBuyers
                    .Where(fb => fb.FormulaId == po.FormulaId)
                    .ToListAsync(ct);

                var currentBuyerIds = currentBuyerAssociations.Select(b => b.BuyerId).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var newBuyer in targetBuyerIds.Except(currentBuyerIds))
                {
                    await _context.AddAsync(new FormulaBuyer
                    {
                        FormulaId = po.FormulaId,
                        BuyerId = newBuyer
                    }, ct);
                }

                var toRemoveBuyers = currentBuyerAssociations.Where(b => !targetBuyerIds.Contains(b.BuyerId)).ToList();
                if (toRemoveBuyers.Any()) _context.FormulaBuyers.RemoveRange(toRemoveBuyers);
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
                .FirstOrDefaultAsync(f => f.FormulaId == id.Value, ct);

            if (formulaPo == null)
                return;

            // 2. 删除所有关联（防止孤儿数据）
            if (formulaPo.FormulaStandardfamilies != null && formulaPo.FormulaStandardfamilies.Any())
            {
                _context.FormulaStandardfamilies.RemoveRange(formulaPo.FormulaStandardfamilies);
            }
            // 删除买家关联
            var buyers = await _context.FormulaBuyers.Where(fb => fb.FormulaId == formulaPo.FormulaId).ToListAsync(ct);
            if (buyers.Any()) _context.FormulaBuyers.RemoveRange(buyers);
            // 3. 删除主记录
            _context.BasicFormulas.Remove(formulaPo);
        }
    }
}
