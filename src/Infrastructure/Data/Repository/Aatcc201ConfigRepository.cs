// ============================================================
// AATCC 201 校准参数仓储 (Aatcc201ConfigRepository)
//
// 职责: DDD 分层中的"持久化边界" —— 把领域聚合根
//       (Domain.Aatcc201Config) 转换为 EF 持久化实体
//       (Persistence.Aatcc201Config, 即 aatcc201_config 表映射) 并执行读写,
//       不包含任何业务规则。
//
// 关键约定（照 PhysicalWeightRecordRepository）:
//   - 写入只做 Add/Update 登记, 不调用 SaveChanges —— 由上层
//     IUnitOfWork 统一提交。
//   - 读取 AsNoTracking(不跟踪): 领域层自行管理状态。
//   - 入库用 Mapster 的 Adapt, 出库用私有 Map(Reconstitute),
//     映射配置集中在 MoistureDryingRateMappingConfig.cs。
//   - 配置表单行: GetAsync 取唯一一行; UpsertAsync 查得到就 Adapt 覆盖
//     已跟踪实体, 查不到就 Add 新增。
// ============================================================
using Mapster;
using Microsoft.EntityFrameworkCore;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MoistureDryingRateContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
// 注意: 不写 using ...Infrastructure.Data.Persistence —— 领域聚合根与持久化实体同名 Aatcc201Config。
// 持久化实体一律用完全限定名 Persistence.Aatcc201Config（由父命名空间 ...Infrastructure.Data 解析），
// 裸名 Aatcc201Config 即无歧义地指向领域聚合根。

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Data.Repository;

/// <summary>
/// AATCC 201 校准参数仓储实现 — Domain↔持久化实体映射, 不自调 SaveChanges(由 IUnitOfWork 统一提交)。
/// 实现 Domain.Contract 里的仓储接口, 通过 IScopedDependency 注册为作用域依赖。
/// </summary>
public class Aatcc201ConfigRepository : IAatcc201ConfigRepository, IScopedDependency
{
    private readonly Persistence.dbContext _context;

    public Aatcc201ConfigRepository(Persistence.dbContext context) { _context = context; }

    /// <summary>读唯一一行配置。AsNoTracking: 只读不跟踪, 省内存且避免意外脏写。</summary>
    public async Task<Aatcc201Config?> GetAsync(CancellationToken ct)
    {
        var po = await _context.Aatcc201Configs.AsNoTracking()
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync(ct);
        return po == null ? null : Map(po);
    }

    /// <summary>
    /// 有则覆盖、无则新增（单行配置 Upsert）。
    /// 新增: Adapt 平铺成 PO 交给 EF AddAsync 跟踪; 覆盖: Adapt 把聚合根当前值
    /// 写到已跟踪实体上, EF 在 SaveChanges 时按主键比较生成 UPDATE。都不落库,
    /// 等 IUnitOfWork.SaveChangesAsync 统一提交。
    /// </summary>
    public async Task UpsertAsync(Aatcc201Config config, CancellationToken ct)
    {
        var po = await _context.Aatcc201Configs.FirstOrDefaultAsync(ct);
        if (po == null)
        {
            await _context.Aatcc201Configs.AddAsync(config.Adapt<Persistence.Aatcc201Config>(), ct);
        }
        else
        {
            config.Adapt(po);
        }
    }

    /// <summary>持久化实体 → 领域聚合根(反向组装)。用 Reconstitute 而不是 Create:
    /// Create 会重置 UpdatedAt 且用于"新建"; 读库回来的历史值应原样还原。</summary>
    private static Aatcc201Config Map(Persistence.Aatcc201Config po) => Aatcc201Config.Reconstitute(
        new Aatcc201ConfigId(po.Id), po.MachineNo,
        po.TempHw1, po.TempHw2, po.TempBoard1, po.TempBoard2, po.Wind1, po.Wind2,
        po.SlopePoint, po.FlatPoint, po.SlopeDgNo, po.SlopeContinueNo, po.SlopeContinueTemp,
        po.SetTemp, po.TempBoard1X, po.TempBoard2X, po.P, po.I, po.D,
        po.UpdatedAt, po.UpdatedBy);
}
