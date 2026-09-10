using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine
{
    /// <summary>
    /// Word 表格编辑工具(OpenXml 层): 供 WordTemplateAdapter 各坐标填充引擎共享的低层表操作。
    /// 与 TextRunHelper 同类——internal static, 只被各引擎内部直接调用, 不注册 DI,
    /// 和成分模板的 DI 服务 WordTemplateEngine 无关(成分/物理克重/干燥速率引擎互相隔离)。
    /// </summary>
    internal static class WordEditEngine
    {
        /// <summary>
        /// 深拷贝整张表(结构+样式), 插到 source 之后, 返回克隆表供调用方继续填格。
        /// 用途: 模板结果表容量不够时按组扩表——如 GB21655 模板每张结果表只有 3 个样品槽,
        ///       6 工位测试需把结果表克隆出第二张(表头样品号由填格逻辑改为 4/5/6)。
        /// 为什么必须先 Clone 再 Insert: OpenXml 一个节点同一时刻只能属于一处文档结构,
        ///       不能把当前表原节点直接当作新节点再插; CloneNode(true)=深拷贝结构+样式,
        ///       克隆后是独立树, 填格互不影响。
        /// </summary>
        internal static Table CloneTableAfter(Table source)
        {
            var clone = (Table)source.CloneNode(true);
            source.InsertAfterSelf(clone);
            return clone;
        }

        /// <summary>
        /// 整页克隆: 把一组连续 body 块级元素(通常=模板一整页: 摘要表+测点表+结果表+Equipment 段+备注表等)
        /// 逐个深拷贝成新页, 默认以分页符段落开头(新页从页首开始, 同一节内分页 → 共享模板页脚/页边距),
        /// 插到文末 body 级 sectPr 之前(schema 规定 sectPr 必须是 w:body 最后一个孩子);
        /// 模板无 body 级 sectPr 时追加到文末(兜底)。返回新克隆的块级元素, 顺序与入参一致——
        /// 调用方按入参元素 ReferenceEquals 即可定位其中某张克隆表。
        /// 用途: 多组报告(如 GB21655 6 工位分两组)要"每组独占一整页", 不是只克隆结果表——
        ///       摘要(报告号/均值)/样品名称/Equipment/备注等页内容都随页重复。
        /// </summary>
        internal static List<OpenXmlElement> ClonePageBlock(
            Body body, IEnumerable<OpenXmlElement> pageElements, bool insertPageBreak = true)
        {
            var clones = new List<OpenXmlElement>();
            var boundary = body.Elements<SectionProperties>().LastOrDefault();

            OpenXmlElement? anchor = null;
            if (insertPageBreak)
            {
                var breakPara = new Paragraph(new Run(new Break { Type = BreakValues.Page }));
                if (boundary != null) { boundary.InsertBeforeSelf(breakPara); anchor = breakPara; }
                else { body.Append(breakPara); anchor = breakPara; }
            }

            foreach (var src in pageElements)
            {
                var clone = src.CloneNode(true);
                if (anchor != null) anchor.InsertAfterSelf(clone);
                else if (boundary != null) boundary.InsertBeforeSelf(clone);
                else body.Append(clone);
                anchor = clone;
                clones.Add(clone);
            }
            return clones;
        }

        /// <summary>
        /// 克隆表内最后一行(连样式: 边框/底纹/字体/合并格)追加到表尾, 并清空每格文本,
        /// 返回新行供调用方填格; 表为空(无行可克隆)返回 null, 调用方自行兜底。
        /// 用途: 模板预留数据行不够时扩容(克重汇总网格/数据表)。
        /// 为什么克隆末行而非新建空白行: 新行会丢模板边框和字号, 报告里出现"没框的行";
        /// 克隆保留完整样式, 只需把文本清掉即可当数据行复用。
        /// 注意: 克隆源是末行, 若模板末行结构特殊(如带合计行), 克隆行样式会不标准;
        /// </summary>
        internal static TableRow? AppendClonedRow(Table table)
        {
            var lastRow = table.Elements<TableRow>().LastOrDefault();
            if (lastRow == null) return null;

            var newRow = (TableRow)lastRow.CloneNode(true);
            table.Append(newRow);

            foreach (var cell in newRow.Elements<TableCell>())
                ClearCellContent(cell);

            return newRow;
        }

        /// <summary>
        /// 清空单元格内全部文本 run, 保留段落结构(每段留一个空 Run 占位, 防样式丢失)。
        /// 供 AppendClonedRow 清空克隆行内容时调用。
        /// </summary>
        private static void ClearCellContent(TableCell cell)
        {
            var paragraphs = cell.Elements<Paragraph>().ToList();

            foreach (var para in paragraphs)
            {
                var runs = para.Elements<Run>().ToList();
                foreach (var run in runs)
                {
                    run.Remove();
                }

                if (!para.HasChildren)
                {
                    para.Append(new Run(new Text("")));
                }
            }
        }

        /// <summary>
        /// 在指定表格后面插入一个空段落（空行）
        /// </summary>
        internal static void InsertEmptyParagraphAfterTable(Table table)
        {
            var parent = table.Parent;
            if (parent == null) return;

            var emptyParagraph = new Paragraph(new Run(new Text("")));

            // 在表格后面插入空段落
            parent.InsertAfter(emptyParagraph, table);
        }

        /// <summary>
        /// 清空行内容 (保留结构)
        /// </summary>
        internal static void ClearRowContent(TableRow row)
        {
            foreach (var cell in row.Elements<TableCell>())
            {
                ClearCellContent(cell);
            }
        }

    }
}
