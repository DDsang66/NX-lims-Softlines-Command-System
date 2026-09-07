using Mapster;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.MoistureDryingRateContext.ValueObj;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Domain.Share.Interface;

namespace NX_lims_Softlines_Command_System.src.Application.Service.MoistureDryingRateContext;

/// <summary>
/// AATCC 201 校准参数应用服务。
/// 流程：GET 读单行配置 → 输出 DTO；PUT 覆盖更新（无则 Create 兜底）→ 仓储 Upsert → IUnitOfWork 提交。
/// 只持久化校准参数本身；PID/修正的串口下发由前端在保存后另行调用，下发失败不回滚库（库是权威）。
/// </summary>
public class Aatcc201ConfigService : IAatcc201ConfigService, IScopedDependency
{
    private readonly IAatcc201ConfigRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public Aatcc201ConfigService(IAatcc201ConfigRepository repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<Result<Aatcc201ConfigDto>> GetAsync(CancellationToken ct)
    {
        // 单行配置：直接读唯一一行（表为空说明 SQL 种子未执行过）
        var config = await _repo.GetAsync(ct);
        if (config == null)
            return Result<Aatcc201ConfigDto>.Fail("未找到 AATCC 201 校准参数，请先执行 CreateAatcc201Config.sql 初始化");

        // 领域聚合根 → 输出 DTO（映射在 MoistureDryingRateMappingConfig）
        return Result<Aatcc201ConfigDto>.Ok(config.Adapt<Aatcc201ConfigDto>());
    }

    /// <inheritdoc />
    public async Task<Result<Aatcc201ConfigDto>> SaveAsync(Aatcc201ConfigSaveDto dto, CancellationToken ct)
    {
        // 空请求直接失败
        if (dto == null)
            return Result<Aatcc201ConfigDto>.Fail("校准参数不能为空");

        // 先读现有配置，决定走"覆盖"还是"新增"两条分支
        var existing = await _repo.GetAsync(ct);

        if (existing != null)
        {
            // 已存在 → 全量覆盖：UpdateAll 应用新值并刷新 UpdatedAt/UpdatedBy，Id 保持不变
            existing.UpdateAll(
                dto.MachineNo,
                dto.TempHw1, dto.TempHw2, dto.TempBoard1, dto.TempBoard2, dto.Wind1, dto.Wind2,
                dto.SlopePoint, dto.FlatPoint, dto.SlopeDgNo, dto.SlopeContinueNo, dto.SlopeContinueTemp,
                dto.SetTemp, dto.TempBoard1X, dto.TempBoard2X, dto.P, dto.I, dto.D,
                dto.UpdatedBy);
            await _repo.UpsertAsync(existing, ct);
        }
        else
        {
            // 表为空（种子未执行或已被删）→ 兜底新建一行
            var created = Aatcc201Config.Create(
                new Aatcc201ConfigId(Guid.NewGuid()),
                dto.MachineNo,
                dto.TempHw1, dto.TempHw2, dto.TempBoard1, dto.TempBoard2, dto.Wind1, dto.Wind2,
                dto.SlopePoint, dto.FlatPoint, dto.SlopeDgNo, dto.SlopeContinueNo, dto.SlopeContinueTemp,
                dto.SetTemp, dto.TempBoard1X, dto.TempBoard2X, dto.P, dto.I, dto.D,
                dto.UpdatedBy);
            await _repo.UpsertAsync(created, ct);
        }

        // 仓储只登记改动，这里统一提交（IUnitOfWork 保证单事务）
        await _unitOfWork.SaveChangesAsync(ct);

        // 回读刚保存的值返回给前端（校准对话框回显最新状态）
        var saved = await _repo.GetAsync(ct);
        return saved == null
            ? Result<Aatcc201ConfigDto>.Fail("保存失败：未读到刚写入的校准参数")
            : Result<Aatcc201ConfigDto>.Ok(saved.Adapt<Aatcc201ConfigDto>());
    }
}
