namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.MoistureDryingRateContext;

/// <summary>
/// 干燥速率报告文件元信息（GET reports 历史报告列表的一项）。
/// 报告以 DOCX 文件存服务器 wwwroot/DocxModel/SaveDocx/DryingRate/，
/// 文件名 = {报告号}_{yyMMddHHmmss}_{mode}.docx，字段从文件名 + 文件属性解析。
/// </summary>
public class ReportFileMeta
{
    /// <summary>文件名（含 .docx 扩展），也是下载接口的标识</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>报告号（手动输入，文件名首段）</summary>
    public string ReportNumber { get; set; } = string.Empty;

    /// <summary>模式：nf5022 | aatcc201（文件名末段）</summary>
    public string Mode { get; set; } = string.Empty;

    /// <summary>文件大小（字节）</summary>
    public long SizeBytes { get; set; }

    /// <summary>生成时间（文件名时间戳解析，解析失败回退文件修改时间）</summary>
    public DateTime GeneratedAt { get; set; }
}
