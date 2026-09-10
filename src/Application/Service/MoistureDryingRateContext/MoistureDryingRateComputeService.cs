using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Application.Interface.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.MoistureDryingRateContext;

/// <summary>
/// 干燥速率权威计算应用服务 —— 计算编排。
/// 职责：接收前端原始时序 → 组装计算输入 → 调静态计算服务 → 映射成输出 DTO。
/// NF5022 是纯计算（公式不依赖配置表）；AATCC 201 多一步"读校准参数 → 构造计算参数"。
/// 不落任何结构化库；计算结果直接回前端刷新，也是 Step3 报告生成的输入。
/// </summary>
public class MoistureDryingRateComputeService : IMoistureDryingRateComputeService, IScopedDependency
{
    private readonly IAatcc201ConfigService _configService;

    public MoistureDryingRateComputeService(IAatcc201ConfigService configService)
    {
        _configService = configService;
    }

    /// <inheritdoc />
    public Result<Nf5022ComputeResultDto> ComputeNf5022(Nf5022ComputeRequestDto dto)
    {
        if (dto.SpaceTimeMin <= 0)
            return Result<Nf5022ComputeResultDto>.Fail("采样间隔须为正整数分钟");

        var method = dto.TestMethod == 0 ? Nf5022TestMethod.Gbt2008 : Nf5022TestMethod.Gbt2023;
        var inputs = dto.Stations
            .Select(s => new Nf5022StationInput(s.FrameWeightMg, s.ClothWeightMg, s.RawWeightMg))
            .ToList();

        var result = DryingRateCalculationService.Calculate(dto.SpaceTimeMin, dto.ResidualMinute, method, inputs);

        return Result<Nf5022ComputeResultDto>.Ok(new Nf5022ComputeResultDto
        {
            SpaceTimeMin = result.SpaceTimeMin,
            ResidualMinute = result.ResidualMinute,
            TestMethod = (int)result.Method,
            Stations = result.Stations.Select(s => new Nf5022StationResultDto
            {
                Station = s.Station,
                Participated = s.Participated,
                WaterMg = s.WaterMg,
                ClothWeightMg = s.ClothWeightMg,
                ResultPoint = s.ResultPoint,
                TimeMin = s.TimeMin,
                RateMgPerHour = s.RateMgPerHour,
                // 决策7：存 mg/h 原值，报告 g/h = 原值 ÷ 1000
                RateGPerHour = Math.Round(s.RateMgPerHour / 1000.0, 3),
                SfclPermille = s.SfclPermille,
                EvaporationCurveMg = s.EvaporationCurveMg?.ToList()
            }).ToList()
        });
    }

    /// <inheritdoc />
    public async Task<Result<Aatcc201ComputeResultDto>> ComputeAatcc201(Aatcc201ComputeRequestDto dto, CancellationToken ct)
    {
        var config = await _configService.GetAsync(ct);
        if (config.IsFailure)
            return Result<Aatcc201ComputeResultDto>.Fail(config.Error);
        var cfg = config.Value!;

        // 配置 → 计算参数。注意单位语义：
        //   TempHw1/2         表里是 0.01℃ 单位（原 mdb 存 5 = 0.05℃ 偏置），直接当 0.01 单位用；
        //   SlopeContinueTemp 表里存 ℃（原软件读入不 ÷100），照原样用。
        var cal = new Aatcc201CalibrationParams(
            cfg.SlopePoint, cfg.FlatPoint, cfg.SlopeDgNo, cfg.SlopeContinueNo,
            (double)cfg.SlopeContinueTemp, (double)cfg.TempHw1, (double)cfg.TempHw2);

        var stations = dto.Stations
            .Select(s => new Aatcc201StationInput(
                s.Station,      // 物理工位 1|2（测试3 复用工位 → 偏置按它取, 不能靠下标）
                s.WaterMl,
                s.Frames.Select(f => new Aatcc201FrameSample(f.SurfaceRaw01, f.BoardRaw01, f.CoverStatus, f.FrameTimeSec)).ToList()))
            .ToList();

        var result = Aatcc201CalculationService.Calculate(cal, stations);

        return Result<Aatcc201ComputeResultDto>.Ok(new Aatcc201ComputeResultDto
        {
            Stations = result.Stations.Select(s => new Aatcc201StationResultDto
            {
                Station = s.Station,
                Participated = s.Participated,
                RateMgPerHour = s.RateMgPerHour,
                // 存 mg/h 原值，报告 g/h = 原值 ÷ 1000
                RateGPerHour = Math.Round(s.RateMgPerHour / 1000.0, 3),
                StartPoint = s.StartPoint,
                EndPoint = s.EndPoint,
                SlopeMaxPoint = s.SlopeMaxPoint,
                FlatMinPoint = s.FlatMinPoint,
                DryingTimeSec = s.DryingTimeSec,
                WaterMl = s.WaterMl,
                // 采纳后温度曲线随结果整体回传, POST report 时作嵌图数据源（未参与工位为 null）
                SurfaceTempSeries = s.TempSeries?.Select(p => new Aatcc201TempPointDto
                {
                    FrameTimeSec = p.FrameTimeSec,
                    SurfaceTemp01 = p.SurfaceTemp01
                }).ToList()
            }).ToList()
        });
    }
}
