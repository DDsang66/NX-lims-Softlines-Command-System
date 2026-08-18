using Mapster;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.BuyerContext.ValueObj;
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

            var buyerIds = await _dbContext.ParamsturctureBuyers
                .Where(pb => pb.ParamStructureId == paramStructurePo.ParamStructureId)
                .Select(pb => new BuyerId(pb.BuyerId))
                .ToListAsync(ct);

            var paramStructure = ParamStructure.Reconstitute(
                id,
                standardFamilyIds,
                ruleIds,
                buyerIds,
                paramStructurePo.FormulaId != null ? new FormulaId(paramStructurePo.FormulaId) : null,
                paramStructurePo.ParamName,
                JsonSerializer.Deserialize<ParamSchema>(paramStructurePo.Schema!,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })!,
                (Status)paramStructurePo.Status,
                paramStructurePo.EngineLayer.HasValue ? (EngineLayer)paramStructurePo.EngineLayer.Value : EngineLayer.Standard,
                paramStructurePo.EffectiveDate
             );


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

            var buyerMapping = await _dbContext.ParamsturctureBuyers
                .Where(pb => idValues.Contains(pb.ParamStructureId))
                .GroupBy(pb => pb.ParamStructureId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(pb => new BuyerId(pb.BuyerId)).ToList(),
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

                var buyerIds = buyerMapping.GetValueOrDefault(paramStructurePo.ParamStructureId, new List<BuyerId>());

                return ParamStructure.Reconstitute(
                    id,
                    standardFamilyIds,
                    ruleIds,
                    buyerIds,
                    paramStructurePo.FormulaId != null ? new FormulaId(paramStructurePo.FormulaId) : null,
                    paramStructurePo.ParamName,
                    JsonSerializer.Deserialize<ParamSchema>(paramStructurePo.Schema!,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        })!,
                    (Status)paramStructurePo.Status,
                    paramStructurePo.EngineLayer.HasValue ? (EngineLayer)paramStructurePo.EngineLayer.Value : EngineLayer.Standard,
                    paramStructurePo.EffectiveDate
                    );
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
            var buyerMapping = await GetBuyerMappingAsync(ids, ct);

            // 内存中组装聚合根
            var result = new List<ParamStructure>(paramStructurePos.Count);
            foreach (var po in paramStructurePos)
            {
                var id = new ParamStructureId(po.ParamStructureId);
                var standardFamilyIds = standardFamilyMapping.GetValueOrDefault(po.ParamStructureId, new List<StandardFamilyId>());
                var ruleIds = ruleMapping.GetValueOrDefault(po.ParamStructureId, new List<ParamRuleId>());

                var buyerIds = buyerMapping.GetValueOrDefault(po.ParamStructureId, new List<BuyerId>());
                var engineLayer = po.EngineLayer.HasValue ? (EngineLayer)po.EngineLayer.Value : EngineLayer.Standard;
                var paramStructure = ParamStructure.Reconstitute(
                    id,
                    standardFamilyIds,
                    ruleIds,
                    buyerIds,
                    po.FormulaId != null ? new FormulaId(po.FormulaId) : null,
                    po.ParamName,
                    JsonSerializer.Deserialize<ParamSchema>(po.Schema!,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })!,
                    (Status)po.Status,
                    engineLayer,
                    po.EffectiveDate
                    );

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
            var buyerMapping = await GetBuyerMappingAsync(ids, ct);

            var result = new List<ParamStructure>(paramStructurePos.Count);
            foreach (var po in paramStructurePos)
            {
                var id = new ParamStructureId(po.ParamStructureId);
                var standardFamilyIds = standardFamilyMapping.GetValueOrDefault(po.ParamStructureId, new List<StandardFamilyId>());
                var ruleIds = ruleMapping.GetValueOrDefault(po.ParamStructureId, new List<ParamRuleId>());
                var buyerIds = buyerMapping.GetValueOrDefault(po.ParamStructureId, new List<BuyerId>());
                var engineLayer = po.EngineLayer.HasValue ? (EngineLayer)po.EngineLayer.Value : EngineLayer.Standard;

                var paramStructure = ParamStructure.Reconstitute(
                    id,
                    standardFamilyIds,
                    ruleIds,
                    buyerIds,
                    po.FormulaId != null ? new FormulaId(po.FormulaId) : null,
                    po.ParamName,
                    JsonSerializer.Deserialize<ParamSchema>(po.Schema!, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    })!,
                    (Status)po.Status,
                    engineLayer,
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

            // 2. 同步标准族关联(与 UpdateAsync 一致,幂等)
            await SyncStandardFamiliesAsync(paramStructurePo.ParamStructureId, paramStructure.StandardFamilyIds, ct);

            // 3. 同步规则关联(同型 bug,一并修)
            await SyncRulesAsync(paramStructurePo.ParamStructureId, paramStructure.ApplicableRuleIds, ct);
        }

        /// <summary>
        /// 更新参数结构
        /// </summary>
        /// <param name="paramStructure"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task UpdateAsync(ParamStructure paramStructure, CancellationToken ct) 
        {
            var id = paramStructure.Id.Value;

            // 1. 查询现有的主表实体
            var existingPo = await _dbContext.BasicParamStructures.FindAsync(id, ct);
            if (existingPo == null)
            {
                throw new Exception("未找到对应的参数结构，无法更新");
            }

            // 2. 更新主表字段
            existingPo.ParamName = paramStructure.ParamName;
            existingPo.FormulaId = paramStructure.FormulaId.Value;
            existingPo.Schema = JsonSerializer.Serialize(paramStructure.Schema, new JsonSerializerOptions { WriteIndented = false });
            existingPo.Status = (byte)paramStructure.Status;
            existingPo.EffectiveDate = paramStructure.EffectiveDate;

            // 3. 同步关联表数据 (StandardFamilies)
            await SyncStandardFamiliesAsync(id, paramStructure.StandardFamilyIds, ct);

            // 4. 同步关联表数据 (Rules)
            await SyncRulesAsync(id, paramStructure.ApplicableRuleIds, ct);

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
        /// 批量获取买家映射字典
        /// </summary>
        private async Task<Dictionary<string, List<BuyerId>>> GetBuyerMappingAsync(List<string> idValues, CancellationToken ct)
        {
            return await _dbContext.ParamsturctureBuyers
                .Where(pb => idValues.Contains(pb.ParamStructureId))
                .GroupBy(pb => pb.ParamStructureId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(pb => new BuyerId(pb.BuyerId)).ToList(),
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
            var buyerMapping = await GetBuyerMappingAsync(paramStructureIds, ct);

            // 3. 重建聚合并返回
            var result = paramStructurePos.Select(po =>
            {
                var id = new ParamStructureId(po.ParamStructureId);
                var standardFamilyIds = standardFamilyMapping.GetValueOrDefault(po.ParamStructureId, new List<StandardFamilyId>());
                var ruleIds = ruleMapping.GetValueOrDefault(po.ParamStructureId, new List<ParamRuleId>());
                var buyers = buyerMapping.GetValueOrDefault(po.ParamStructureId, new List<BuyerId>());
                var engineLayer = po.EngineLayer.HasValue ? (EngineLayer)po.EngineLayer.Value : EngineLayer.Standard;

                return ParamStructure.Reconstitute(
                    id,
                    standardFamilyIds,
                    ruleIds,
                    buyers,
                    po.FormulaId != null ? new FormulaId(po.FormulaId) : null,
                    po.ParamName,
                    JsonSerializer.Deserialize<ParamSchema>(po.Schema!,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        })!,
                    (Status)po.Status,
                    engineLayer,
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

            // 批量查询买家映射
            var buyerMapping = await GetBuyerMappingAsync(paramStructureIds, ct);

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
                var buyers = buyerMapping.GetValueOrDefault(item.ParamStructure.ParamStructureId, new List<BuyerId>());
                var engineLayer = item.ParamStructure.EngineLayer.HasValue ? (EngineLayer)item.ParamStructure.EngineLayer.Value : EngineLayer.Standard;

                return ParamStructure.Reconstitute(
                    id,
                    item.StandardFamilyIds,
                    item.RuleIds,
                    buyers,
                    item.ParamStructure.FormulaId != null ? new FormulaId(item.ParamStructure.FormulaId) : null,
                    item.ParamStructure.ParamName,
                    JsonSerializer.Deserialize<ParamSchema>(item.ParamStructure.Schema!,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        })!,
                    (Status)item.ParamStructure.Status,
                    engineLayer,
                    item.ParamStructure.EffectiveDate);
            });

            return paramStructures;
        }



        /// <summary>
        /// 同步参数结构与标准族的关联关系
        /// </summary>
        private async Task SyncStandardFamiliesAsync(string paramStructureId, IEnumerable<StandardFamilyId> latestFamilyIds, CancellationToken ct)
        {
            // 获取当前数据库中存在的关联记录
            var existingRelations = await _dbContext.ParamsturctureStandardfamilies
                .Where(af => af.ParamStructureId == paramStructureId)
                .ToListAsync(ct);

            var latestIdValues = latestFamilyIds.Select(id => id.Value).ToList();

            // 删除不再需要的关联
            var toRemove = existingRelations.Where(er => !latestIdValues.Contains(er.IdStandardFamily)).ToList();
            _dbContext.ParamsturctureStandardfamilies.RemoveRange(toRemove);

            // 添加新增的关联
            var existingIdValues = existingRelations.Select(er => er.IdStandardFamily).ToList();
            foreach (var newId in latestIdValues.Except(existingIdValues))
            {
                await _dbContext.ParamsturctureStandardfamilies.AddAsync(new ParamsturctureStandardfamily
                {
                    ParamStructureId = paramStructureId,
                    IdStandardFamily = newId
                }, ct);
            }
        }

        /// <summary>
        /// 同步参数结构与规则的关联关系
        /// </summary>
        private async Task SyncRulesAsync(string paramStructureId, IEnumerable<ParamRuleId> latestRuleIds, CancellationToken ct)
        {
            // 获取当前数据库中存在的关联记录
            var existingRelations = await _dbContext.BasicParamRules
                .Where(ar => ar.ParamStructureId == paramStructureId)
                .ToListAsync(ct);

            var latestIdValues = latestRuleIds.Select(id => id.Value).ToList();

            // 删除不再需要的关联
            var toRemove = existingRelations.Where(er => !latestIdValues.Contains(er.RuleId)).ToList();
            _dbContext.BasicParamRules.RemoveRange(toRemove);

            // 添加新增的关联
            var existingIdValues = existingRelations.Select(er => er.RuleId).ToList();
            foreach (var newId in latestIdValues.Except(existingIdValues))
            {
                await _dbContext.BasicParamRules.AddAsync(new BasicParamRule
                {
                    ParamStructureId = paramStructureId,
                    RuleId = newId
                }, ct);
            }
        }
    }
}
