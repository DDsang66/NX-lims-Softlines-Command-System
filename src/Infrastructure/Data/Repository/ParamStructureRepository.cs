using Mapster;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Enums;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;
using System.Text.Json;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class ParamStructureRepository:IParamStructureRepository,IScopedDependency
    {
        private readonly dbContext _dbContext;

        public ParamStructureRepository(dbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 查询结构
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<ParamStructure> GetByIdAsync(ParamStructureId id, CancellationToken ct) 
        {
            var paramStructurePo = await _dbContext.BasicParamStructures.FindAsync(id.Value, ct);

            if (paramStructurePo == null) 
                throw new Exception("未找到对应的参数结构");

            //查询所有paramStructurePo对应的StandardFamilyId
            var standardFamilyIds = await  _dbContext.ParamsturctureStandardfamilies
                .Where(af => af.ParamStructureId == paramStructurePo.ParamStructureId)
                .Select(af => new StandardFamilyId(af.IdStandardFamily))
                .ToListAsync(ct);

            var ruleIds = await  _dbContext.BasicParamRules
                .Where(br => br.ParamStructureId == paramStructurePo.ParamStructureId)
                .Select(br => new ParamRuleId(br.RuleId))
                .ToListAsync(ct);

            var paramStructure = ParamStructure.Reconstitute(
                id,
                standardFamilyIds,
                ruleIds,
                new FormulaId(paramStructurePo.FormulaId),
                paramStructurePo.ParamName,
                JsonSerializer.Deserialize<ParamSchema>(paramStructurePo.Schema!, 
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })!,
                (Status)paramStructurePo.Status,
                paramStructurePo.EffectiveDate);


            return paramStructure;
        }

        /// <summary>
        /// 根据标准族查询结构
        /// </summary>
        /// <param name="standardFamilyId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ParamStructure>> GetByFamilyIdAsync(StandardFamilyId standardFamilyId, CancellationToken ct)
        {
            // 1. 查询关联的 ParamStructure ID 列表
            var paramStructureIds = await _dbContext.ParamsturctureStandardfamilies
                .Where(af => af.IdStandardFamily == standardFamilyId)
                .Select(af => new ParamStructureId(af.ParamStructureId))
                .ToListAsync(ct);

            if (!paramStructureIds.Any())
            {
                return Enumerable.Empty<ParamStructure>();
            }

            var idValues = paramStructureIds.Select(id => id.Value).ToList();

            // 2. 批量查询 ParamStructure PO 实体
            var paramStructurePos = await _dbContext.BasicParamStructures
                .Where(ps => idValues.Contains(ps.ParamStructureId))
                .ToListAsync(ct);

            // 3. 批量查询关联的 StandardFamilyIds (根据你的实际表结构调整)
            var standardFamilyMapping = await _dbContext.ParamsturctureStandardfamilies
                .Where(af => idValues.Contains(af.ParamStructureId))
                .GroupBy(af => af.ParamStructureId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(af => new StandardFamilyId(af.IdStandardFamily)).ToList(),
                    ct);

            // 4. 批量查询关联的 RuleIds (根据你的实际表结构调整，假设有 ParamStructureRule 关系表)
            var ruleMapping = await _dbContext.BasicParamRules
                .Where(ar => idValues.Contains(ar.ParamStructureId))
                .GroupBy(ar => ar.ParamStructureId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(ar => new ParamRuleId(ar.RuleId)).ToList(),
                    ct);

            // 5. 遍历 PO 列表，批量重建聚合根
            var paramStructures = paramStructurePos.Select(paramStructurePo =>
            {
                var id = new ParamStructureId(paramStructurePo.ParamStructureId); // 假设 ID 的构建方式

                // 从字典中安全获取关联ID集合，如果不存在则赋空集合
                var standardFamilyIds = standardFamilyMapping.GetValueOrDefault(paramStructurePo.ParamStructureId, new List<StandardFamilyId>());
                var ruleIds = ruleMapping.GetValueOrDefault(paramStructurePo.ParamStructureId, new List<ParamRuleId>());

                return ParamStructure.Reconstitute(
                    id,
                    standardFamilyIds,
                    ruleIds,
                    new FormulaId(paramStructurePo.FormulaId),
                    paramStructurePo.ParamName,
                    JsonSerializer.Deserialize<ParamSchema>(paramStructurePo.Schema!,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        })!,
                    (Status)paramStructurePo.Status,
                    paramStructurePo.EffectiveDate);
            });

            return paramStructures;
        }

        /// <summary>
        /// 获取所有参数结构
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<List<ParamStructure>> GetAllAsync(CancellationToken ct) 
        {
            // 获取所有主表实体的 ID
            var paramStructurePos = await _dbContext.BasicParamStructures.ToListAsync(ct);
            if (!paramStructurePos.Any())
            {
                return new List<ParamStructure>();
            }

            var ids = paramStructurePos.Select(p => p.ParamStructureId).ToList();
            var paramStructureIds = ids.Select(id => new ParamStructureId(id)).ToList();

            // 批量获取关联表数据
            var standardFamilyMapping = await GetStandardFamilyMappingAsync(ids, ct);
            var ruleMapping = await GetRuleMappingAsync(ids, ct);

            // 内存中组装聚合根
            var result = new List<ParamStructure>(paramStructurePos.Count);
            foreach (var po in paramStructurePos)
            {
                var id = new ParamStructureId(po.ParamStructureId);
                var standardFamilyIds = standardFamilyMapping.GetValueOrDefault(po.ParamStructureId, new List<StandardFamilyId>());
                var ruleIds = ruleMapping.GetValueOrDefault(po.ParamStructureId, new List<ParamRuleId>());

                var paramStructure = ParamStructure.Reconstitute(
                    id,
                    standardFamilyIds,
                    ruleIds,
                    new FormulaId(po.FormulaId),
                    po.ParamName,
                    JsonSerializer.Deserialize<ParamSchema>(po.Schema!,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })!,
                    (Status)po.Status,
                    po.EffectiveDate);

                result.Add(paramStructure);
            }

            return result;
        }




        /// <summary>
        /// 根据参数名称查询结构
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ParamStructure>> GetByParamName(string paramName, CancellationToken ct)
        {
            var paramStructurePos = await _dbContext.BasicParamStructures
                .Where(p => p.ParamName == paramName)
                .ToListAsync(ct);

            if (!paramStructurePos.Any())
            {
                return Enumerable.Empty<ParamStructure>();
            }

            var ids = paramStructurePos.Select(p => p.ParamStructureId).ToList();
            var paramStructureIds = ids.Select(id => new ParamStructureId(id)).ToList();

            var standardFamilyMapping = await GetStandardFamilyMappingAsync(ids, ct);
            var ruleMapping = await GetRuleMappingAsync(ids, ct);

            var result = new List<ParamStructure>(paramStructurePos.Count);
            foreach (var po in paramStructurePos)
            {
                var id = new ParamStructureId(po.ParamStructureId);
                var standardFamilyIds = standardFamilyMapping.GetValueOrDefault(po.ParamStructureId, new List<StandardFamilyId>());
                var ruleIds = ruleMapping.GetValueOrDefault(po.ParamStructureId, new List<ParamRuleId>());

                var paramStructure = ParamStructure.Reconstitute(
                    id,
                    standardFamilyIds,
                    ruleIds,
                    new FormulaId(po.FormulaId),
                    po.ParamName,
                    JsonSerializer.Deserialize<ParamSchema>(po.Schema!, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })!,
                    (Status)po.Status,
                    po.EffectiveDate);

                result.Add(paramStructure);
            }

            return result;
        }

        /// <summary>
        /// 添加参数结构
        /// </summary>
        /// <param name="paramStructure"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task AddAsync(ParamStructure paramStructure, CancellationToken ct)
        {
            // 1. 转换并保存主表
            var paramStructurePo = paramStructure.Adapt<BasicParamStructure>();

            await _dbContext.AddAsync(paramStructurePo, ct);
        }

        /// <summary>
        /// 更新参数结构
        /// </summary>
        /// <param name="paramStructure"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task UpdateAsync(ParamStructure paramStructure, CancellationToken ct) 
        {
            await Task.CompletedTask;
        }

        /// <summary>
        /// 批量获取标准族映射字典
        /// </summary>
        private async Task<Dictionary<string, List<StandardFamilyId>>> GetStandardFamilyMappingAsync(List<string> idValues, CancellationToken ct)
        {
            return await _dbContext.ParamsturctureStandardfamilies
                .Where(af => idValues.Contains(af.ParamStructureId))
                .GroupBy(af => af.ParamStructureId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(af => new StandardFamilyId(af.IdStandardFamily)).ToList(),
                    ct);
        }

        /// <summary>
        /// 批量获取规则映射字典
        /// </summary>
        private async Task<Dictionary<string, List<ParamRuleId>>> GetRuleMappingAsync(List<string> idValues, CancellationToken ct)
        {
            return await _dbContext.BasicParamRules
                .Where(ar => idValues.Contains(ar.ParamStructureId))
                .GroupBy(ar => ar.ParamStructureId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(ar => new ParamRuleId(ar.RuleId)).ToList(),
                    ct);
        }

        /// <summary>
        /// 根据多个公式ID批量查询参数结构（实现）
        /// </summary>
        /// <param name="formulaIds">公式ID 列表</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>参数结构列表（去重）</returns>
        public async Task<IEnumerable<ParamStructure>> GetByFormulaIdsAsync(
            List<FormulaId> formulaIds,
            CancellationToken ct)
        {
            if (formulaIds == null || !formulaIds.Any())
                return Enumerable.Empty<ParamStructure>();

            var formulaIdValues = formulaIds.Select(f => f.Value).ToList();

            // 1. 直接查询 BasicParamStructures 中 FormulaId 匹配的记录
            var paramStructurePos = await _dbContext.BasicParamStructures
                .AsNoTracking()
                .Where(ps => formulaIdValues.Contains(ps.FormulaId))
                .ToListAsync(ct);

            if (!paramStructurePos.Any())
                return Enumerable.Empty<ParamStructure>();

            var paramStructureIds = paramStructurePos.Select(ps => ps.ParamStructureId).ToList();

            // 2. 批量获取关联映射
            var standardFamilyMapping = await GetStandardFamilyMappingAsync(paramStructureIds, ct);
            var ruleMapping = await GetRuleMappingAsync(paramStructureIds, ct);

            // 3. 重建聚合并返回
            var result = paramStructurePos.Select(po =>
            {
                var id = new ParamStructureId(po.ParamStructureId);
                var standardFamilyIds = standardFamilyMapping.GetValueOrDefault(po.ParamStructureId, new List<StandardFamilyId>());
                var ruleIds = ruleMapping.GetValueOrDefault(po.ParamStructureId, new List<ParamRuleId>());

                return ParamStructure.Reconstitute(
                    id,
                    standardFamilyIds,
                    ruleIds,
                    new FormulaId(po.FormulaId),
                    po.ParamName,
                    JsonSerializer.Deserialize<ParamSchema>(po.Schema!,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        })!,
                    (Status)po.Status,
                    po.EffectiveDate);
            });

            return result;
        }


        /// <summary>
        /// 根据多个标准族ID批量查询参数结构（优化版）
        /// </summary>
        /// <param name="standardFamilyIds">标准族ID列表</param>
        /// <param name="ct">取消令牌</param>
        /// <returns>参数结构列表（去重）</returns>
        public async Task<IEnumerable<ParamStructure>> GetByFamilyIdsAsync(
            List<StandardFamilyId> standardFamilyIds,
            CancellationToken ct)
        {
            if (standardFamilyIds == null || !standardFamilyIds.Any())
                return Enumerable.Empty<ParamStructure>();

            var familyIdValues = standardFamilyIds.Select(id => id.Value).ToList();

            // 使用 JOIN 一次性查询所有数据
            var query = from ps in _dbContext.BasicParamStructures
                        join af in _dbContext.ParamsturctureStandardfamilies
                            on ps.ParamStructureId equals af.ParamStructureId
                        where familyIdValues.Contains(af.IdStandardFamily)
                        select new
                        {
                            ParamStructure = ps,
                            StandardFamilyId = af.IdStandardFamily
                        };

            var rawData = await query.ToListAsync(ct);

            if (!rawData.Any())
                return Enumerable.Empty<ParamStructure>();

            // 获取所有 ParamStructure ID
            var paramStructureIds = rawData
                .Select(x => x.ParamStructure.ParamStructureId)
                .Distinct()
                .ToList();

            // 批量查询规则（单独查询，因为规则表可能较大）
            var ruleMapping = await _dbContext.BasicParamRules
                .Where(ar => paramStructureIds.Contains(ar.ParamStructureId))
                .GroupBy(ar => ar.ParamStructureId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(ar => new ParamRuleId(ar.RuleId)).ToList(),
                    ct);

            // 按 ParamStructureId 分组
            var groupedData = rawData
                .GroupBy(x => x.ParamStructure.ParamStructureId)
                .Select(g => new
                {
                    ParamStructure = g.First().ParamStructure,
                    StandardFamilyIds = g
                        .Select(x => new StandardFamilyId(x.StandardFamilyId))
                        .Distinct()
                        .ToList(),
                    RuleIds = ruleMapping.GetValueOrDefault(g.Key, new List<ParamRuleId>())
                })
                .ToList();

            // 重建聚合根
            var paramStructures = groupedData.Select(item =>
            {
                var id = new ParamStructureId(item.ParamStructure.ParamStructureId);

                return ParamStructure.Reconstitute(
                    id,
                    item.StandardFamilyIds,
                    item.RuleIds,
                    new FormulaId(item.ParamStructure.FormulaId),
                    item.ParamStructure.ParamName,
                    JsonSerializer.Deserialize<ParamSchema>(item.ParamStructure.Schema!,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        })!,
                    (Status)item.ParamStructure.Status,
                    item.ParamStructure.EffectiveDate);
            });

            return paramStructures;
        }
    }
}
