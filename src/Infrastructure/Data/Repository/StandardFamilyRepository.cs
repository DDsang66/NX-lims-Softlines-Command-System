using DocumentFormat.OpenXml.Office2010.Excel;
using Mapster;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.FormulaContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamRuleContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.ParamStructureContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;
using System.Linq;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class StandardFamilyRepository:IStandardFamilyRepository, IScopedDependency
    {
        private readonly dbContext _dbContext;

        public StandardFamilyRepository(dbContext dbContext) 
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Add new StandardFamily
        /// </summary>
        /// <param name="standardFamily"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task AddAsync(StandardFamily standardFamily, CancellationToken ct)
        {
            var standardFamilyPo = standardFamily.Adapt<BasicStandardFamily>();

            await _dbContext.AddAsync(standardFamilyPo, ct);

            await Task.CompletedTask;
        }

        /// <summary>
        /// 更新标准族
        /// </summary>
        /// <param name="standardFamily"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task UpdateAsync(StandardFamily standardFamily, CancellationToken ct)
        {
            var standardFamilyPo = await _dbContext.FindAsync<BasicStandardFamily>(standardFamily.Id.Value, ct);

            if (standardFamilyPo == null)
                throw new Exception($"标准族 {standardFamily.Id.Value} 不存在");

            standardFamilyPo.Adapt(standardFamily);

            await Task.CompletedTask;
        }

        /// <summary>
        /// 移除标准族
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task RemoveAsync(StandardFamilyId id, CancellationToken ct)
        {
            var standardFamilyPo = new BasicStandardFamily { IdStandardFamily = id.Value };

            _dbContext.Attach(standardFamilyPo);

            _dbContext.Remove(standardFamilyPo);

            await Task.CompletedTask;
        }

        /// <summary>
        /// 根据id查询标准族
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<StandardFamily?> GetByIdAsync(StandardFamilyId id, CancellationToken ct)
        {
            var standardFamilyPo = await  _dbContext.FindAsync<BasicStandardFamily>(id.Value,ct);

            if (standardFamilyPo == null)
                return null;

            var standardIdTask =  _dbContext.BasicStandards
                .Where(x => x.StandardFamilyCodeId == id.Value)
                .Select(x => x.IdStandard)
                .ToListAsync(ct);

            var fomulaIdTask =   _dbContext.FormulaStandardfamilies
                .Where(x => x.IdStandardFamily == id.Value)
                .Select(x=>x.FormulaId)
                .ToListAsync(ct);

            var structureIdTask =  _dbContext.ParamsturctureStandardfamilies
                .Where(x => x.IdStandardFamily == id.Value)
                .Select(x => x.ParamStructureId)
                .ToListAsync(ct);

            var ruleIdTask =  _dbContext.BasicParamRules
                .Where(x => x.StandardFamilyCodeId == id.Value)
                .Select(x => x.RuleId)
                .ToListAsync(ct);

            // 等待所有查询完成
            await Task.WhenAll(standardIdTask, fomulaIdTask, structureIdTask, ruleIdTask);

            // 从 Task 中获取结果
            var standardIdList = await standardIdTask;
            var fomulaIdList = await fomulaIdTask;
            var structureIdList = await structureIdTask;
            var ruleIdList = await ruleIdTask;

            return StandardFamily.Reconstitute(
                new StandardFamilyId(standardFamilyPo.IdStandardFamily),
                standardFamilyPo.StandardFamilyCode,
                standardIdList.Select(id => new StandardId(id)).ToList(),
                fomulaIdList.Select(id => new FormulaId(id)).ToList(),
                structureIdList.Select(id => new ParamStructureId(id)).ToList(),
                ruleIdList.Select(id => new ParamRuleId(id)).ToList(),
                standardFamilyPo.Version,
                standardFamilyPo.EffectiveDate
            );
        }


        /// <summary>
        /// 根据标准id查询标准族
        /// </summary>
        /// <param name="standaraId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<StandardFamily> GetByStandardIdAsync(StandardId standaraId,CancellationToken ct) 
        {
            var standardFamilyId = await _dbContext.BasicStandards
                .Where(x => x.IdStandard == standaraId.Value)
                .Select(x => x.StandardFamilyCodeId)
                .FirstOrDefaultAsync(ct);

            var standardFamilyPo = await _dbContext.FindAsync<BasicStandardFamily>(standardFamilyId, ct);

            if (standardFamilyPo == null)
                return null;

            var standardIdTask = _dbContext.BasicStandards
                .Where(x => x.StandardFamilyCodeId == standardFamilyId)
                .Select(x => x.IdStandard)
                .ToListAsync(ct);

            var fomulaIdTask = _dbContext.FormulaStandardfamilies
                .Where(x => x.IdStandardFamily == standardFamilyId)
                .Select(x => x.FormulaId)
                .ToListAsync(ct);

            var structureIdTask = _dbContext.ParamsturctureStandardfamilies
                .Where(x => x.IdStandardFamily == standardFamilyId)
                .Select(x => x.ParamStructureId)
                .ToListAsync(ct);

            var ruleIdTask = _dbContext.BasicParamRules
                .Where(x => x.StandardFamilyCodeId == standardFamilyId)
                .Select(x => x.RuleId)
                .ToListAsync(ct);

            // 等待所有查询完成
            await Task.WhenAll(standardIdTask, fomulaIdTask, structureIdTask, ruleIdTask);

            // 从 Task 中获取结果
            var standardIdList = await standardIdTask;
            var fomulaIdList = await fomulaIdTask;
            var structureIdList = await structureIdTask;
            var ruleIdList = await ruleIdTask;

            return StandardFamily.Reconstitute(
                new StandardFamilyId(standardFamilyPo.IdStandardFamily),
                standardFamilyPo.StandardFamilyCode,
                standardIdList.Select(id => new StandardId(id)).ToList(),
                fomulaIdList.Select(id => new FormulaId(id)).ToList(),
                structureIdList.Select(id => new ParamStructureId(id)).ToList(),
                ruleIdList.Select(id => new ParamRuleId(id)).ToList(),
                standardFamilyPo.Version,
                standardFamilyPo.EffectiveDate
            );
        }

    }
}
