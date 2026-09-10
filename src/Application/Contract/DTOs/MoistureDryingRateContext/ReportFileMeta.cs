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

    /// <summary>
    /// 样品名称（读报告 docx 内 "Sample" 表头格回填, 不是来自文件名）。
    /// AATCC 201 历史列表用它分辨同报告号下不同时间做的样品; 合并报告 = 各样品名以顿号拼接。
    /// 非 aatcc201 模式 / 文件结构不符 / 读取失败 → 空串(列表照常返回, 不因单个坏文件失败)。
    /// </summary>
    public string SampleName { get; set; } = string.Empty;

    /// <summary>文件大小（字节）</summary>
    public long SizeBytes { get; set; }

    /// <summary>生成时间（文件名时间戳解析，解析失败回退文件修改时间）</summary>
    public DateTime GeneratedAt { get; set; }
}
