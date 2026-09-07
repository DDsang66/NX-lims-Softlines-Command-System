using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.MoistureDryingRateContext;

/// <summary>
/// AATCC 201 校准参数仓储接口。
/// 配置表是单行结构（无外键、无子实体），所以不走通用 IRepository&lt;T,TId,TValue&gt; 模板
/// （其 Add/Update/GetById 形状对不上），而是照 PhysicalWeightRecordRepository 的定制写法：
///   - GetAsync     —— 读唯一一行，服务层无需传入 Id；
///   - UpsertAsync  —— 有则覆盖、无则插入，服务层不关心是第几次。
/// 继承 IScopedDependency 仅用于 Scrutor 自动注册（AddAutoRegister 扫描），与领域约束无关。
/// </summary>
public interface IAatcc201ConfigRepository : IScopedDependency
{
    /// <summary>读唯一一行校准配置，表为空返回 null。</summary>
    Task<Aatcc201Config?> GetAsync(CancellationToken ct);

    /// <summary>有则覆盖更新、无则新增。只登记改动，不调用 SaveChanges（由上层 IUnitOfWork 统一提交）。</summary>
    Task UpsertAsync(Aatcc201Config config, CancellationToken ct);
}
