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
            var standardPo  = standard.Adapt<BasicStandard>();

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

            // 使用 Mapster 的 Adapt 方法将领域模型的变更覆盖到已追踪的 PO 上
            // 注意：这里不能直接 standard.Adapt<BasicStandard>()，因为会生成新对象
            // 需要将源对象映射到已有目标对象
            standard.Adapt(standardPo);
        }


        /// <summary>
        /// 批量更新标准 (依赖 EF Core 变更追踪)
        /// </summary>
        public async Task UpdateRangeAsync(IEnumerable<Standard> standards, CancellationToken ct)
        {
            var standardsList = standards.ToList();
            var ids = standardsList.Select(s => s.IdStandard.Value).ToList();

            var existingPos = await _context.BasicStandards
                .Where(s => ids.Contains(s.IdStandard))
                .ToListAsync(ct);

            var existingDict = existingPos.ToDictionary(s => s.IdStandard);

            foreach (var standard in standardsList)
            {
                if (!existingDict.TryGetValue(standard.IdStandard.Value, out var po))
                    throw new Exception($"标准 {standard.IdStandard.Value} 不存在");

                // 同 UpdateAsync，映射到已有对象
                standard.Adapt(po);
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

            // 修复原代码的空引用风险
            if (standardPo == null) return null;

            return standardPo.Adapt<Standard>();
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
            if (!idValues.Any()) return Enumerable.Empty<Standard>();

            var standardPos = await _context.BasicStandards
                .AsNoTracking()
                .Where(s => idValues.Contains(s.IdStandard))
                .ToListAsync(ct);

            // 批量映射，极大简化代码
            return standardPos.Adapt<List<Standard>>();
        }


        /// <summary>
        /// 获取标准列表
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        public async Task<IEnumerable<Standard>> GetStandardListAsync(CancellationToken ct) 
        {
            var standardPos = await _context.BasicStandards
                   .AsNoTracking()
                   .ToListAsync(ct);

            // 批量映射
            return standardPos.Adapt<List<Standard>>();
        }
    }
}

