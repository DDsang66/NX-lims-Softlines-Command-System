using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Application.Interface.MoistureDryingRateContext;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.src.Web_API;

/// <summary>
/// 水分干燥速率 API — NF5022 重量式 + AATCC 201 加热板法共用控制器，按 mode 分发。
/// 报告文件名带秒级时间戳，不同模式/报告号即不同文件，互不覆盖；
/// 校准参数单行配置，并发保存 last-write-wins。
/// 当前提供：AATCC 201 校准参数读写 + 权威计算（compute/nf5022、compute/aatcc201）
///           + 报告生成（report/nf5022、report/aatcc201）+ 历史报告文件列表/下载。
/// </summary>
[ApiController]
[Route("api/[Controller]")]
public class MoistureDryingRateController : ControllerBase
{
    // ── 依赖注入：控制器只声明"需要谁"，由 DI 容器构造并注入，保持薄层（只做路由+委托） ──

    /// <summary>校准参数读写服务 —— GET/PUT aatcc201-config 委托给它</summary>
    private readonly IAatcc201ConfigService _configService;

    /// <summary>报告文件存储服务 —— GET reports 列表/下载委托给它</summary>
    private readonly IReportFileStore _reportStore;

    /// <summary>权威计算服务 —— POST compute/nf5022 与 compute/aatcc201 委托给它</summary>
    private readonly IMoistureDryingRateComputeService _computeService;

    /// <summary>报告生成服务 —— POST report/nf5022 与 report/aatcc201 委托给它</summary>
    private readonly IMoistureDryingRateReportService _reportService;

    /// <summary>
    /// 依赖注入入口：每次请求创建控制器时，DI 容器按参数类型从容器取出对应实现并注入。
    /// 三个服务实现类都标了 IScopedDependency（Scrutor 自动注册），此处无需手写注册。
    /// 注入后存进私有字段，供下方各端点调用。
    /// </summary>
    public MoistureDryingRateController(
        IAatcc201ConfigService configService,
        IReportFileStore reportStore,
        IMoistureDryingRateComputeService computeService,
        IMoistureDryingRateReportService reportService)
    {
        _configService = configService;
        _reportStore = reportStore;
        _computeService = computeService;
        _reportService = reportService;
    }

    // ============ AATCC 201 校准参数 ============

    /// <summary>读 AATCC 201 校准参数（校准对话框打开时回显当前值）</summary>
    [HttpGet("aatcc201-config")]
    public Task<Result<Aatcc201ConfigDto>> GetConfig(CancellationToken ct)
        => _configService.GetAsync(ct);

    /// <summary>
    /// 保存 AATCC 201 校准参数（覆盖更新，写 updated_at/updated_by）。
    /// 注意：PID/修正的串口下发由前端保存后另行调用，下发失败不回滚本接口已存的值。
    /// </summary>
    [HttpPut("aatcc201-config")]
    public Task<Result<Aatcc201ConfigDto>> SaveConfig([FromBody] Aatcc201ConfigSaveDto dto, CancellationToken ct)
        => _configService.SaveAsync(dto, ct);

    // ============ 权威计算（不落结构化库） ============

    /// <summary>NF5022 权威计算：前端测试结束 POST 原始时序 → 返回逐工位 滴水量/蒸发时间/干燥速率/残留率。</summary>
    [HttpPost("compute/nf5022")]
    public Result<Nf5022ComputeResultDto> ComputeNf5022([FromBody] Nf5022ComputeRequestDto dto)
        => _computeService.ComputeNf5022(dto);

    /// <summary>AATCC 201 权威计算：读校准参数 + 原始时序 → 返回逐工位 起点/终点/干燥速率/干燥时间。</summary>
    [HttpPost("compute/aatcc201")]
    public Task<Result<Aatcc201ComputeResultDto>> ComputeAatcc201([FromBody] Aatcc201ComputeRequestDto dto, CancellationToken ct)
        => _computeService.ComputeAatcc201(dto, ct);

    // ============ 报告生成（不落结构化库, DOCX 存服务器报告目录） ============

    /// <summary>NF5022 报告生成：样品头 + 权威结果 → DryingRate.docx 存服务器, 返回下载链接。</summary>
    [HttpPost("report/nf5022")]
    public Result<DocxUrlResponseDto> GenerateNf5022Report([FromBody] Nf5022ReportRequestDto dto)
        => _reportService.GenerateNf5022(dto);

    /// <summary>AATCC 201 报告生成：样品头 + 权威结果 → Aatcc201.docx 存服务器, 返回下载链接（回显校准参数需读库）。</summary>
    [HttpPost("report/aatcc201")]
    public async Task<Result<DocxUrlResponseDto>> GenerateAatcc201Report([FromBody] Aatcc201ReportRequestDto dto, CancellationToken ct)
        => await _reportService.GenerateAatcc201(dto, ct);

    // ============ 历史报告文件（不落结构化库） ============

    /// <summary>按模式 + 报告号关键字列历史报告文件（数据查询入口）</summary>
    [HttpGet("reports")]
    public Result<List<ReportFileMeta>> ListReports([FromQuery] string mode, [FromQuery] string? keyword)
        => Result<List<ReportFileMeta>>.Ok(_reportStore.ListReports(mode, keyword));

    /// <summary>下载指定报告文件（文件名由 ListReports 提供）</summary>
    [HttpGet("reports/{fileName}")]
    public IActionResult Download(string fileName)
    {
        // 解析完整路径并做路径穿越校验，文件不存在返回 404
        var path = _reportStore.ResolvePath(fileName);
        if (path == null)
            return NotFound(new { success = false, message = "报告文件不存在" });

        return PhysicalFile(
            path,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            fileDownloadName: fileName,
            enableRangeProcessing: true
        );
    }
}
