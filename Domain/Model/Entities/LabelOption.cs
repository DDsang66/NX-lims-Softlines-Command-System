namespace NX_lims_Softlines_Command_System.Domain.Model.Entities;

/// <summary>
/// 标签选项（前端单选框选项来源）
/// JudgmentLabelRemark: A184-A188
/// LanguageLabelRemark: A162-A176
/// </summary>
public partial class LabelOption
{
    public int Id { get; set; }

    /// <summary>"Judgment" 或 "Language"</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>排序</summary>
    public int SortOrder { get; set; }

    /// <summary>选项文本</summary>
    public string Text { get; set; } = string.Empty;
}
