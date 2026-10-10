namespace NX_lims_Softlines_Command_System.src.Domain.Contract.Util
{
    public sealed class DocxMergeOptions
    {
        /// <summary>是否覆盖已存在的输出文件</summary>
        public bool OverwriteOutput { get; init; } = true;

        /// <summary>是否复制源文档的页眉页脚（默认 true）</summary>
        public bool CopyHeaderFooter { get; init; } = true;

        /// <summary>追加新节时的分节符类型</summary>
        public SectionBreakType DefaultSectionBreak { get; init; } = SectionBreakType.NextPage;

        /// <summary>是否在合并完成后输出 document.xml 等调试信息（仅诊断用）</summary>
        public bool DumpXmlForDiagnostics { get; init; } = false;

        /// <summary>调试 XML 输出目录，为空则写 Console</summary>
        public string? DumpDirectory { get; init; }
    }

    public enum SectionBreakType
    {
        NextPage,
        Continuous,
        EvenPage,
        OddPage
    }
}
