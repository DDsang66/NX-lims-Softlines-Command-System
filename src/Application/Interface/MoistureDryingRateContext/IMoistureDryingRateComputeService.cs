using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Interface.MoistureDryingRateContext;

/// <summary>
/// 干燥速率权威计算应用服务接口（POST compute/nf5022 | compute/aatcc201）。
/// 前端测试结束把原始时序 POST 过来，后端按原软件公式算权威结果，不落结构化库。
/// NF5022 纯计算；AATCC 201 需先读 aatcc201_config 校准参数构造计算参数。
/// 通过 IScopedDependency 自动注册。
/// </summary>
public interface IMoistureDryingRateComputeService : IScopedDependency
{
    /// <summary>
    /// NF5022 权威计算：逐工位算 滴水量/蒸发时间/干燥速率/残留率。
    /// 无数据库依赖（NF5022 公式不读配置表），纯计算。
    /// </summary>
    Result<Nf5022ComputeResultDto> ComputeNf5022(Nf5022ComputeRequestDto dto);

    /// <summary>
    /// AATCC 201 权威计算：读校准参数 → 构造计算参数 → 逐工位算 起点/终点/干燥速率/干燥时间。
    /// 校准参数是唯一的数据依赖（空表返回失败提示）。
    /// </summary>
    Task<Result<Aatcc201ComputeResultDto>> ComputeAatcc201(Aatcc201ComputeRequestDto dto, CancellationToken ct);
}
