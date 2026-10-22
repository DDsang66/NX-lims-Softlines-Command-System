namespace NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine.WordTemplateAdapter
{
    /// <summary>
    /// Sample area 扩容的行为配置。
    /// 之所以把这些规则抽成配置而非写死，是因为 OpenXML 无法获取真实页面布局（剩余空间、行高），
    /// 分页与合并单元格的处理口径都必须能按实际模板实测后调整。
    /// </summary>
    public sealed class AreaExpansionOptions
    {
        /// <summary>
        /// 单页可容纳的表格行数估算阈值（默认 50）。
        /// 扩容后 structure 表的总行数超过该值时才插入分页符，让整表从新页开始。
        /// 该值不是真实排版结果，需按 A4 模板实测行高后调整。
        /// </summary>
        public int RowsPerPage { get; set; } = 50;

        /// <summary>
        /// 行数超过 <see cref="RowsPerPage"/> 时，是否在 structure 表之前插入分页符。
        /// false 表示只依赖行级的 CantSplit（行内不断页），表格本身仍可能被页面切开。
        /// </summary>
        public bool InsertPageBreakWhenOverflow { get; set; } = true;

        /// <summary>
        /// 锚点行存在纵向合并单元格（vMerge continue）时的处理策略。
        /// 默认 Reject，避免在不确定语义的情况下产出格式错乱的 docx。
        /// </summary>
        public VerticalMergePolicy VerticalMerge { get; set; } = VerticalMergePolicy.Reject;
    }

    /// <summary>
    /// 纵向合并单元格在 area 扩容时的处理策略。
    /// </summary>
    public enum VerticalMergePolicy
    {
        /// <summary>
        /// 检测到纵向合并（续行）立即抛异常，交人工确认模板。
        /// 这是默认值：宁可阻断，也不要产出 Word 可能报损坏的文档。
        /// </summary>
        Reject = 0,

        /// <summary>
        /// 克隆后清除新增行的 vMerge，使每个新增区各自独立成格。
        /// 仅在确认新增区不需要参与纵向合并时使用。
        /// </summary>
        SplitOnClone = 1
    }
}
