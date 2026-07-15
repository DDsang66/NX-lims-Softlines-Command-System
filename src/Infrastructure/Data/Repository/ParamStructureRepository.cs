using Mapster;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.ParamEngineContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
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

            var formulaIds = await  _dbContext.ParamstructureFormulas
                .Where(af => af.ParamStructureId == paramStructurePo.ParamStructureId)
                .Select(af => new FormulaId(af.FormulaId))
                .ToListAsync(ct);

            var ruleIds = await  _dbContext.BasicParamRules
                .Where(br => br.ParamStructureId == paramStructurePo.ParamStructureId)
                .Select(br => new ParamRuleId(br.RuleId))
                .ToListAsync(ct);

            var paramStructure = ParamStructure.Reconstitute(
                id,
                standardFamilyIds,
                formulaIds,
                paramStructurePo.ParamName,
                JsonSerializer.Deserialize<ParamSchema>(paramStructurePo.Schema!, 
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })!,
                ruleIds,
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
            return null;
        }

        /// <summary>
        /// 根据参数名称查询结构
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IEnumerable<ParamStructure>> GetByParamName(string paramName, CancellationToken ct)
        {
            return null;
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

            //外键关联索引通过发布领域方法触发单独的仓储去保存
            //
            // 2. 处理与 StandardFamily 的关联
            //foreach (var familyId in paramStructure.StandardFamilyIds.Where(id => id != null))
            //{
            //    if (!await _dbContext.ParamsturctureStandardfamilies
            //        .AnyAsync(af =>
            //            af.ParamStructureId == paramStructurePo.ParamStructureId &&
            //            af.IdStandardFamily == familyId!.Value,
            //            ct))
            //    {
            //        await _dbContext.AddAsync(new ParamsturctureStandardfamily
            //        {
            //            ParamStructureId = paramStructurePo.ParamStructureId,
            //            IdStandardFamily = familyId!.Value
            //        }, ct);
            //    }
            //}

            // 3. 处理与 Formula 的关联
            //foreach (var formulaId in paramStructure.FormulaIds.Where(id => id != null))
            //{
            //    if (!await _dbContext.ParamstructureFormulas
            //        .AnyAsync(af =>
            //            af.ParamStructureId == paramStructurePo.ParamStructureId &&
            //            af.FormulaId == formulaId!.Value,
            //            ct))
            //    {
            //        await _dbContext.AddAsync(new ParamstructureFormula
            //        {
            //            FormulaId = formulaId!.Value,
            //            ParamStructureId = paramStructurePo.ParamStructureId
            //        }, ct);
            //    }
            //}
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

    }
}
