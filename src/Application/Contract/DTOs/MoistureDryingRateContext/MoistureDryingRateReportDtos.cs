namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MoistureDryingRateContext;

// ============================================================
// 干燥速率"报告生成"请求 DTO（POST report/nf5022 | report/aatcc201）
//
// 定位:
//   - 前端测试结束 → POST compute 拿到权威结果(Nf5022ComputeResultDto / Aatcc201ComputeResultDto)
//     → 再 POST report 把"样品头 + 该结果"整体交后端生成 DOCX。
//   - 结果 DTO 整体内嵌(Result 字段)而不是打平, 保证报告与前端显示的权威结果是同一份,
//     后端不需要重算, 也就没有"第二次计算结果不一致"的问题。
//   - 响应复用 DocxUrlResponseDto(fileKey/fileName/downloadUrl), 照 PhysicalWeightReportService。
//   - 曲线图数据已在结果 DTO 里(NF5022: EvaporationCurveMg); AATCC 的温度序列目前不在
//     结果 DTO 里, 报告嵌图待图表库决策时一并定(见服务注释)。
// ============================================================

/// <summary>NF5022 报告生成请求：样品头 + 权威计算结果（内嵌, 不重算）。</summary>
public class Nf5022ReportRequestDto
{
    /// <summary>报告号（手动输入, 文件名首段, 与 compute 请求一致）</summary>
    public string ReportNumber { get; set; } = string.Empty;

    /// <summary>样品名称</summary>
    public string SampleName { get; set; } = string.Empty;

    /// <summary>环境温度（原软件文本输入）</summary>
    public string Temperature { get; set; } = string.Empty;

    /// <summary>环境湿度（原软件文本输入）</summary>
    public string Humidity { get; set; } = string.Empty;

    /// <summary>compute/nf5022 返回的权威结果（内嵌整体, 报告照它生成）</summary>
    public Nf5022ComputeResultDto? Result { get; set; }
}

/// <summary>AATCC 201 报告生成请求：样品头 + 权威计算结果（内嵌, 不重算）。</summary>
public class Aatcc201ReportRequestDto
{
    /// <summary>报告号（手动输入, 文件名首段, 与 compute 请求一致）</summary>
    public string ReportNumber { get; set; } = string.Empty;

    /// <summary>样品名称</summary>
    public string SampleName { get; set; } = string.Empty;

    /// <summary>环境温度（原软件文本输入）</summary>
    public string Temperature { get; set; } = string.Empty;

    /// <summary>环境湿度（原软件文本输入）</summary>
    public string Humidity { get; set; } = string.Empty;

    /// <summary>compute/aatcc201 返回的权威结果（内嵌整体, 报告照它生成）</summary>
    public Aatcc201ComputeResultDto? Result { get; set; }
}

/// <summary>
/// AATCC 201 合并报告请求 —— 历史报告界面勾选同一报告号下的多个样品文件, 合成一份报告。
/// 数据源是历史 docx 本身（生成时刻没有另存结构化结果）, 由引擎 ReadReport 解析回来。
/// </summary>
public class Aatcc201CombineRequestDto
{
    /// <summary>所选历史报告文件名（含 .docx, 来自 GET reports 列表; 必须同报告号, 至少 1 份）</summary>
    public List<string> FileNames { get; set; } = new();
}

/// <summary>
/// 解析一份历史 AATCC 报告 docx 得到的内容（引擎 ReadReport 返回）。
/// Samples/Charts 的形状与 Aatcc201SampleBlockModel 一致, 服务可直接拼进合并填充模型。
/// </summary>
public class Aatcc201ParsedReport
{
    /// <summary>报告号（摘要表 R0 col1 读出）</summary>
    public string ReportNumber { get; set; } = string.Empty;

    /// <summary>环境温度（页脚读出; 模板横线未填过 → null）</summary>
    public string? Temperature { get; set; }

    /// <summary>环境湿度（页脚读出; 模板横线未填过 → null）</summary>
    public string? Humidity { get; set; }

    /// <summary>该文件里实际有数据的样品块（空白 Sample 表不产出块; 合并产物再合并时逐表都算样品）</summary>
    public List<Aatcc201SampleBlockModel> Samples { get; set; } = new();

    /// <summary>正文里的曲线图 PNG（正文只有追加在文末的曲线图; 表头/页脚 logo 在别的部件, 不会进来）</summary>
    public List<byte[]> Charts { get; set; } = new();
}
