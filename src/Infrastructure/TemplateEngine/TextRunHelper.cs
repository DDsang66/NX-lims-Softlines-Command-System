using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine
{
    /// <summary>
    /// OpenXml 文本工具: 把含 \n 的文本拆成多 run + &lt;w:br/&gt;, 供成分/物理克重引擎共用。
    /// </summary>
    internal static class TextRunHelper
    {
        /// <summary>
        /// 处理含 \n 的文本：拆分为多段，Run 间插入 &lt;w:br/&gt; 实现 Word 换行
        /// </summary>
        internal static void InsertTextWithLineBreaks(string text, Run firstRun)
        {
            var lines = text.Split('\n');
            firstRun.Append(new Text(lines[0]) { Space = SpaceProcessingModeValues.Preserve });

            if (lines.Length <= 1) return;

            OpenXmlElement insertAfter = firstRun;
            for (int i = 1; i < lines.Length; i++)
            {
                var brRun = new Run(new Break());
                var textRun = new Run(
                    firstRun.RunProperties?.CloneNode(true) as RunProperties,
                    new Text(lines[i]) { Space = SpaceProcessingModeValues.Preserve });

                insertAfter = insertAfter.InsertAfterSelf(brRun);
                brRun.InsertAfterSelf(textRun);
                insertAfter = textRun;
            }
        }
    }
}
