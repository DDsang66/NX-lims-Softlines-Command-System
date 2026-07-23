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
            var standardFamilyPo = standardFamily.Adapt<BasicStandardFamily>();

            _dbContext.Update(standardFamilyPo);

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
            var result = await _dbContext.BasicStandardFamilies
                          .Where(sf => sf.IdStandardFamily == id)
                          .Select(sf => new
                          {
                              Family = sf,
                              StandardIds = _dbContext.BasicStandards
                                  .Where(s => s.StandardFamilyCodeId == sf.IdStandardFamily)
                                  .Select(s => s.IdStandard)
                                  .ToList(),
                              FormulaIds = _dbContext.FormulaStandardfamilies
                                  .Where(f => f.IdStandardFamily == sf.IdStandardFamily)
                                  .Select(f => f.FormulaId)
                                  .ToList(),
                              StructureIds = _dbContext.ParamsturctureStandardfamilies
                                  .Where(p => p.IdStandardFamily == sf.IdStandardFamily)
                                  .Select(p => p.ParamStructureId)
                                  .ToList()
                          })
                          .FirstOrDefaultAsync(ct);

            if (result == null) return null;

            return StandardFamily.Reconstitute(
                new StandardFamilyId(result.Family.IdStandardFamily),
                result.Family.StandardFamilyCode,
                result.StandardIds.Select(id => new StandardId(id)).ToList(),
                result.FormulaIds.Select(id => new FormulaId(id)).ToList(),
                result.StructureIds.Select(id => new ParamStructureId(id)).ToList(),
                result.Family.Version,
                result.Family.EffectiveDate
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

            var result = await _dbContext.BasicStandardFamilies
                .Where(sf => sf.IdStandardFamily == standardFamilyId)
                .Select(sf => new
                {
                    Family = sf,
                    StandardIds = _dbContext.BasicStandards
                        .Where(s => s.StandardFamilyCodeId == sf.IdStandardFamily)
                        .Select(s => s.IdStandard)
                        .ToList(),
                    FormulaIds = _dbContext.FormulaStandardfamilies
                        .Where(f => f.IdStandardFamily == sf.IdStandardFamily)
                        .Select(f => f.FormulaId)
                        .ToList(),
                    StructureIds = _dbContext.ParamsturctureStandardfamilies
                        .Where(p => p.IdStandardFamily == sf.IdStandardFamily)
                        .Select(p => p.ParamStructureId)
                        .ToList()
                })
                .FirstOrDefaultAsync(ct);

            if (result == null) return null;

            return StandardFamily.Reconstitute(
                new StandardFamilyId(result.Family.IdStandardFamily),
                result.Family.StandardFamilyCode,
                result.StandardIds.Select(id => new StandardId(id)).ToList(),
                result.FormulaIds.Select(id => new FormulaId(id)).ToList(),
                result.StructureIds.Select(id => new ParamStructureId(id)).ToList(),
                result.Family.Version,
                result.Family.EffectiveDate
            );
        }

    }
}
