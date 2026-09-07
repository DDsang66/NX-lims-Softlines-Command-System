using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.MoistureDryingRateContext;

/// <summary>
/// AATCC 201 校准参数应用服务接口（读写 aatcc201_config 单行配置）。
/// 控制器通过该服务访问校准参数，业务编排在实现层（仓储 + IUnitOfWork）。
/// 通过 IScopedDependency 自动注册。
/// </summary>
public interface IAatcc201ConfigService : IScopedDependency
{
    /// <summary>读当前校准参数；表为空（未初始化）返回失败提示。</summary>
    Task<Result<Aatcc201ConfigDto>> GetAsync(CancellationToken ct);

    /// <summary>保存校准参数：已存在则覆盖，不存在则新增（种子兜底），提交后返回最新值。</summary>
    Task<Result<Aatcc201ConfigDto>> SaveAsync(Aatcc201ConfigSaveDto dto, CancellationToken ct);
}
