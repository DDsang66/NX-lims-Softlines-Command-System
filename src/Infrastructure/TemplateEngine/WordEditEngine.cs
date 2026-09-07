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
        
    }
}
