using System.Linq;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Application.Interface.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Application.Service.MoistureDryingRateContext;

/// <summary>
/// 干燥速率报告生成应用服务 — 编排。
/// 职责: 校验请求 → 把"样品头 + 权威计算结果"映射成引擎填充模型 → 复制模板到位 →
///       调引擎就地填充 → 返回下载链接（文件由 ReportFileStore 落到报告目录）。
/// 与 PhysicalWeightReportService 的区别: 结果不重算——计算结果由前端 POST 进来,
/// 也就没有"报告与页面显示不一致"的问题。唯一例外: AATCC 报告回显"校准参数"行时
/// 读 aatcc201_config 当前值（静态配置回显, 不是重算结果）。
/// 曲线图: DryingRateChartService 用结果 DTO 里的曲线数据(EvaporationCurveMg / SurfaceTempSeries)
///         生成 PNG 挂到模型 ChartImagePng, 引擎嵌入"曲线图"占位段; 无参与工位 → null(不插图)。
/// </summary>
public class MoistureDryingRateReportService : IMoistureDryingRateReportService, IScopedDependency
{
    private readonly IDryingRateDocxEngine _nf5022Engine;
    private readonly IAatcc201DocxEngine _aatcc201Engine;
    private readonly IReportFileStore _reportStore;
    private readonly IAatcc201ConfigService _configService;

    public MoistureDryingRateReportService(
        IDryingRateDocxEngine nf5022Engine,
        IAatcc201DocxEngine aatcc201Engine,
        IReportFileStore reportStore,
        IAatcc201ConfigService configService)
    {
        _nf5022Engine = nf5022Engine;
        _aatcc201Engine = aatcc201Engine;
        _reportStore = reportStore;
        _configService = configService;
    }

    /// <inheritdoc />
    public Result<DocxUrlResponseDto> GenerateNf5022(Nf5022ReportRequestDto dto)
    {
        if (dto?.Result?.Stations == null || dto.Result.Stations.Count == 0)
            return Result<DocxUrlResponseDto>.Fail("没有可生成的 NF5022 计算结果");
        if (!dto.Result.Stations.Any(s => s.Participated))
            return Result<DocxUrlResponseDto>.Fail("本测试没有实际参与的工位，不能生成报告");
        if (string.IsNullOrWhiteSpace(dto.ReportNumber))
            return Result<DocxUrlResponseDto>.Fail("报告号不能为空");

        // 每个参与工位独立一张蒸发曲线 PNG;
        // 渲染异常不阻塞报告: 单张失败就跳过该样品, 引擎照已有图按序追加
        var chartPngs = new List<byte[]>();
        foreach (var s in dto.Result.Stations.Where(s => s.Participated))
        {
            try
            {
                var png = DryingRateChartService.RenderNf5022StationChart(s.Station, s.EvaporationCurveMg, dto.Result.SpaceTimeMin);
                if (png != null) chartPngs.Add(png);
            }
            catch { /* 单个样品图渲染失败 → 跳过, 不拖垮整份报告 */ }
        }

        var model = new DryingRateReportFillModel
        {
            ReportNumber = dto.ReportNumber,
            SampleName = dto.SampleName,
            Temperature = dto.Temperature,
            Humidity = dto.Humidity,
            TestMethod = dto.Result.TestMethod == 0 ? "GBT 21655.1 2008" : "GBT 21655.1 2023",
            SpaceTimeMin = dto.Result.SpaceTimeMin,
            ResidualMinute = dto.Result.ResidualMinute,
            Stations = dto.Result.Stations.Select(s => new DryingRateStationRowModel
            {
                Station = s.Station,
                Participated = s.Participated,
                WaterMg = s.WaterMg,
                ClothWeightMg = s.ClothWeightMg,
                TimeMin = s.TimeMin,
                RateMgPerHour = s.RateMgPerHour,
                RateGPerHour = s.RateGPerHour,
                SfclPermille = s.SfclPermille,
                // GB21655 报告: 按模板 3min 网格填 Δmi/mi + 算回归斜率的数据源
                EvaporationCurveMg = s.EvaporationCurveMg
            }).ToList(),
            ChartPngs = chartPngs
        };

        return Generate(dto.ReportNumber, "nf5022", Path.Combine("Common_PHY", "PHY_GB21655_DryingRate.docx"), model, _nf5022Engine.FillReport);
    }

    /// <inheritdoc />
    public async Task<Result<DocxUrlResponseDto>> GenerateAatcc201(Aatcc201ReportRequestDto dto, CancellationToken ct)
    {
        if (dto?.Result?.Stations == null || dto.Result.Stations.Count == 0)
            return Result<DocxUrlResponseDto>.Fail("没有可生成的 AATCC 201 计算结果");
        if (!dto.Result.Stations.Any(s => s.Participated))
            return Result<DocxUrlResponseDto>.Fail("本测试没有实际参与的工位，不能生成报告");
        if (string.IsNullOrWhiteSpace(dto.ReportNumber))
            return Result<DocxUrlResponseDto>.Fail("报告号不能为空");

        // 报告回显校准参数: 读库当前配置组一行文字（静态配置回显, 不重算结果）
        var config = await _configService.GetAsync(ct);
        if (config.IsFailure)
            return Result<DocxUrlResponseDto>.Fail("读取校准参数失败: " + config.Error);

        // 温度曲线 PNG 渲染异常不阻塞报告: 失败置 null, 引擎见 null 不插图
        byte[]? chartPng;
        try { chartPng = DryingRateChartService.RenderAatcc201TemperatureChart(dto.Result); }
        catch { chartPng = null; }

        var model = new Aatcc201ReportFillModel
        {
            ReportNumber = dto.ReportNumber,
            SampleName = dto.SampleName,
            Temperature = dto.Temperature,
            Humidity = dto.Humidity,
            CalibrationText = BuildCalibrationText(config.Value!),
            Stations = dto.Result.Stations.Select(s => new Aatcc201StationRowModel
            {
                Station = s.Station,
                Participated = s.Participated,
                WaterMl = s.WaterMl,
                DryingTimeSec = s.DryingTimeSec,
                RateMgPerHour = s.RateMgPerHour,
                RateGPerHour = s.RateGPerHour,
                StartPoint = s.StartPoint,
                EndPoint = s.EndPoint,
                // 采纳后温度曲线已随结果 DTO 回传
                SurfaceTempSeries = s.SurfaceTempSeries
            }).ToList(),
            // 温度曲线 PNG: 用参与工位的 SurfaceTempSeries 生成; 无参与工位/渲染失败 → null(引擎不插图)
            ChartImagePng = chartPng
        };

        return Generate(dto.ReportNumber, "aatcc201", Path.Combine("Common_PHY", "PHY_AATCC201_DryingRate.docx"), model, _aatcc201Engine.FillReport);
    }

    /// <summary>
    /// 把当前校准配置组一行文字（照校准对话框显示的值, 数值原样, 便于实验室核对现场设置）。
    /// </summary>
    private static string BuildCalibrationText(Aatcc201ConfigDto cfg)
        => $"设定温度 {cfg.SetTemp:0.#}℃; PID {cfg.P:0.#}/{cfg.I:0.#}/{cfg.D:0.#}; " +
           $"板修正 {cfg.TempBoard1X:0.#}/{cfg.TempBoard2X:0.#}℃; " +
           $"面温偏置 {cfg.TempHw1:0.##}/{cfg.TempHw2:0.##}; 风速偏置 {cfg.Wind1:0.##}/{cfg.Wind2:0.##}; " +
           $"斜坡 {cfg.SlopePoint}/{cfg.FlatPoint}/{cfg.SlopeDgNo}/{cfg.SlopeContinueNo} 点 {cfg.SlopeContinueTemp:0.#}℃";

    /// <summary>
    /// 公共生成流程: 复制模板到位 → 引擎填充 → 返回下载链接。
    /// 模板缺失/引擎抛异常都转成 Fail（模板缺失会在复制时抛出清晰错误）。
    /// </summary>
    private Result<DocxUrlResponseDto> Generate<TModel>(
        string reportNumber, string mode, string templateName, TModel model, Action<string, TModel> fill)
    {
        try
        {
            string targetPath = _reportStore.PrepareReportFile(Path.Combine("DocxModel", templateName), reportNumber, mode);
            fill(targetPath, model);
            string fileName = Path.GetFileName(targetPath);
            return Result<DocxUrlResponseDto>.Ok(new DocxUrlResponseDto
            {
                fileKey = fileName,
                fileName = fileName,
                downloadUrl = $"/api/MoistureDryingRate/reports/{fileName}"
            });
        }
        catch (Exception ex)
        {
            return Result<DocxUrlResponseDto>.Fail("生成报告失败: " + ex.Message);
        }
    }
}
