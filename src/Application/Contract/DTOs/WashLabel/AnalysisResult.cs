namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.WashLabel;

public class AnalysisResult
{
    public string RawText { get; set; } = string.Empty;
    public List<StructuredTable> Tables { get; set; } = new();
    public List<CareSymbol> Symbols { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
}

public class CareSymbol
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

/// <summary>
/// 从 rawText 中提取的 Markdown 表格结构化数据
/// </summary>
public class StructuredTable
{
    public List<string> Headers { get; set; } = new();
    public List<Dictionary<string, string>> Rows { get; set; } = new();
}
