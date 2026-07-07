using AutoMapper;
using Mapster;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.Domain.Aggregeates.Standard;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.ParamEngineContext.StandardFamilyContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.Standard.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.Data.Persistence;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository
{
    public class StandardRepository : IStandardRepository, IScopedDependency
    {
        private readonly dbContext _context;

        public StandardRepository(dbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 新增标准 — 聚合根的所有行批量插入 BasicStandards 表
        /// </summary>
        /// <param name="standard"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task AddAsync(Standard standard, CancellationToken ct)
        {
            BasicStandard standardPo = new BasicStandard
            {
                IdStandard = standard.IdStandard.Value,
                StandardCode = standard.StandardCode,
                StandardCodeNameEn = standard.StandardCodeNameEn,
                StandardCodeNameChn = standard.StandardCodeNameChn,
                Status = (byte)standard.Status,
                StandardFamilyCodeId = standard.StandardFamilyCode.Value
            };

            await _context.AddAsync(standardPo, ct);//由工作单元统一提交
        }

        /// <summary>
        /// 更新标准 — 按 line.Id 逐行覆盖 BasicStandards 表
        /// </summary>
        /// <param name="standard"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task UpdateAsync(Standard standard, CancellationToken ct)
        {
            var standardPo = await _context.FindAsync<BasicStandard>(standard.IdStandard.Value, ct);

            if (standardPo == null)
                throw new Exception($"标准 {standard.IdStandard.Value} 不存在");

            // 直接修改属性，EF 自动追踪变更
            standardPo.StandardCode = standard.StandardCode;
            standardPo.StandardCodeNameEn = standard.StandardCodeNameEn;
            standardPo.StandardCodeNameChn = standard.StandardCodeNameChn;
            standardPo.Status = (byte)standard.Status;
            standardPo.StandardFamilyCodeId = standard.StandardFamilyCode.Value;
        }


        /// <summary>
        /// 批量更新标准 (依赖 EF Core 变更追踪)
        /// </summary>
        public async Task UpdateRangeAsync(IEnumerable<Standard> standards, CancellationToken ct)
        {
            var standardsList = standards.ToList();
            var ids = standardsList.Select(s => s.IdStandard.Value).ToList();

            // 1. 查询现有记录
            var existingPos = await _context.BasicStandards
                .Where(s => ids.Contains(s.IdStandard))
                .ToListAsync(ct);

            var existingDict = existingPos.ToDictionary(s => s.IdStandard);

            // 2. 逐个映射更新
            foreach (var standard in standardsList)
            {
                if (!existingDict.TryGetValue(standard.IdStandard.Value, out var po))
                    throw new Exception($"标准 {standard.IdStandard.Value} 不存在");

                // 手动映射字段
                po.StandardCode = standard.StandardCode;
                po.StandardCodeNameEn = standard.StandardCodeNameEn;
                po.StandardCodeNameChn = standard.StandardCodeNameChn;
                po.Status = (byte)standard.Status;
                po.StandardFamilyCodeId = standard.StandardFamilyCode.Value;

                // EF 自动追踪变更，不需要显式调用 Update
            }
        }

        /// <summary>
        /// 删除标准 — 按 line.Id 逐行删除 BasicStandards 表
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task RemoveAsync(StandardId id, CancellationToken ct) 
        {
            var standardPo = new BasicStandard { IdStandard = id.Value };

            _context.Attach(standardPo);
            _context.Remove(standardPo);

        }

        /// <summary>
        /// 根据 Id 获取标准
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<Standard?> GetByIdAsync(StandardId id, CancellationToken ct)
        {
            var standardPo = await _context.FindAsync<BasicStandard>(id.Value, ct);

            var standard = Standard.Reconstitute(
                new StandardId(standardPo.IdStandard),
                standardPo.StandardCode,
                standardPo.StandardCodeNameEn,
                standardPo.StandardCodeNameChn,
                standardPo.Status == 1 ? Status.Active 
                : standardPo.Status== 2 ? Status.Draft 
                : Status.Deprecated,
                new StandardFamilyId(standardPo.StandardFamilyCodeId)
            );

            return standard;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ids"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Standard>> GetByIdsAsync(IEnumerable<StandardId> ids, CancellationToken ct)
        {
            var idValues = ids.Select(id => id.Value).ToList();

            if (!idValues.Any())
                return Enumerable.Empty<Standard>();

            var standardPos = await _context.BasicStandards
                .AsNoTracking()
                .Where(s => idValues.Contains(s.IdStandard))
                .ToListAsync(ct);

            var standards = new List<Standard>();

            foreach (var standardPo in standardPos)
            {
                var standard = Standard.Reconstitute(
                    new StandardId(standardPo.IdStandard),
                    standardPo.StandardCode,
                    standardPo.StandardCodeNameEn,
                    standardPo.StandardCodeNameChn,
                    standardPo.Status == 1 ? Status.Active
                    : standardPo.Status == 2 ? Status.Draft
                    : Status.Deprecated,
                    new StandardFamilyId(standardPo.StandardFamilyCodeId)
                );

                standards.Add(standard);
            }

            return standards;
        }


        /// <summary>
        /// 获取标准列表
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Standard>> GetStandardListAsync(CancellationToken ct) 
        {
            var standardPos = await _context.BasicStandards
                   .AsNoTracking()           // 只读查询，提升性能
                   .ToListAsync(ct);

            var standards = new List<Standard>();

            foreach (var standardPo in standardPos)
            {
                var standard = Standard.Reconstitute(
                    new StandardId(standardPo.IdStandard),
                    standardPo.StandardCode,
                    standardPo.StandardCodeNameEn,
                    standardPo.StandardCodeNameChn,
                    standardPo.Status == 1 ? Status.Active
                    : standardPo.Status == 2 ? Status.Draft
                    : Status.Deprecated,
                    new StandardFamilyId(standardPo.StandardFamilyCodeId)
                );
                standards.Add(standard);
            }

            return standards;
        }
    }
}

