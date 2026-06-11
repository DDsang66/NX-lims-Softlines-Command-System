using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine
{
    /// <summary>
    /// Word 模板引擎
    /// 仅封装底层操作功能，不涉及业务逻辑
    /// </summary>
    public class WordTemplateEngine:IScopedDependency
    {

        /// <summary>
        /// 构造函数
        /// </summary>
        public WordTemplateEngine()
        {

        }

        /// <summary>
        /// 根据书签替换文本（支持正文、页眉、页脚）
        /// </summary>
        /// <param name="filePath">Word文档路径</param>
        /// <param name="bookmarkValues">书签名-值字典</param>
        public void ReplaceText(string filePath, Dictionary<string, string> bookmarkValues)
        {
            if (!bookmarkValues.Any()) return;

            using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, true))
            {
                // 替换正文中的书签
                ReplaceBookmarksInPart(doc.MainDocumentPart!, bookmarkValues);

                // 替换所有页眉中的书签
                foreach (var headerPart in doc.MainDocumentPart!.HeaderParts)
                {
                    ReplaceBookmarksInPart(headerPart, bookmarkValues);
                }

                // 替换所有页脚中的书签
                foreach (var footerPart in doc.MainDocumentPart.FooterParts)
                {
                    ReplaceBookmarksInPart(footerPart, bookmarkValues);
                }

                // 保存更改
                doc.MainDocumentPart.Document!.Save();
            }
        }


        /// <summary>
        /// 在指定部件中替换书签
        /// <param name="part">Word文档的任意部件（正文/页眉/页脚/脚注等）</param>
        /// <param name="values">书签名-值字典</param>
        /// </summary>
        private void ReplaceBookmarksInPart(OpenXmlPart part, Dictionary<string, string> values)
        {
            var root = part.RootElement;
            if (root == null) return;

            // 获取所有书签开始标记
            var bookmarkStarts = root.Descendants<BookmarkStart>().ToList();

            foreach (var bookmarkStart in bookmarkStarts)
            {
                string bookmarkName = bookmarkStart.Name!;

                // 检查是否需要替换此书签
                if (!values.ContainsKey(bookmarkName)) continue;

                // 查找对应的书签结束标记（通过ID匹配）
                var bookmarkEnd = root.Descendants<BookmarkEnd>()
                    .FirstOrDefault(b => b.Id == bookmarkStart.Id);

                if (bookmarkEnd == null) continue;

                // 执行替换
                ReplaceBookmarkContent(bookmarkStart, bookmarkEnd, values[bookmarkName]);
            }
        }

        /// <summary>
        /// 替换书签内的内容
        /// </summary>
        private void ReplaceBookmarkContent(BookmarkStart start, BookmarkEnd end, string newText)
        {
            // 获取父段落
            var parentPara = start.Ancestors<Paragraph>().FirstOrDefault();
            if (parentPara == null) return;

            // 获取书签范围内的所有元素
            var elementsBetween = GetElementsBetween(start, end).ToList();

            // 删除旧内容（保留书签标记本身）
            foreach (var elem in elementsBetween)
            {
                elem.Remove();
            }

            // 在书签开始后插入新文本
            var newRun = new Run(
                new RunProperties(),
                new Text(newText) { Space = SpaceProcessingModeValues.Preserve }
            );

            start.InsertAfterSelf(newRun);
        }

        /// <summary>
        /// 获取两个元素之间的所有元素
        /// </summary>
        private IEnumerable<OpenXmlElement> GetElementsBetween(BookmarkStart start, BookmarkEnd end)
        {
            // 在同一段落内查找
            var parent = start.Parent;
            if (parent != end.Parent) yield break; // 跨段落的书签不处理

            bool foundStart = false;
            foreach (var elem in parent!.ChildElements.ToList())
            {
                if (elem == start)
                {
                    foundStart = true;
                    continue;
                }

                if (elem == end) break;

                if (foundStart) yield return elem;
            }
        }

        /// <summary>
        /// 从数据库获取书签名和值（预留方法）
        /// </summary>
        private Dictionary<string, string> GetBookmarksFromDatabase()
        {
            // TODO: 实现数据库查询
            // 示例：
            // return dbContext.Bookmarks.ToDictionary(b => b.Name, b => b.Value);

            return new Dictionary<string, string>();
        }


        /// <summary>
        /// 在指定书签位置插入图片（支持正文、页眉、页脚）
        /// </summary>
        /// <param name="filePath">Word文档路径</param>
        /// <param name="bookmarkName">书签名</param>
        /// <param name="imageId">图片在文档中的rId（需外部先通过 AddImagePart 添加）</param>
        /// <param name="imageName">图片文件名（用于描述）</param>
        /// <param name="widthEmu">图片宽度（EMU），默认约6英寸</param>
        /// <param name="heightEmu">图片高度（EMU），默认约4英寸</param>
        public void ReplaceWithImage(string filePath, string bookmarkName,
            string imageId, string imageName,
            long widthEmu = 5486400, long heightEmu = 3657600)
        {
            using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, true))
            {
                var bookmark = doc.MainDocumentPart!.Document.Body
                    .Descendants<BookmarkStart>()
                    .FirstOrDefault(b => b.Name == bookmarkName);

                if (bookmark == null) return;

                var bookmarkEnd = doc.MainDocumentPart.Document.Body
                    .Descendants<BookmarkEnd>()
                    .FirstOrDefault(b => b.Id == bookmark.Id);

                if (bookmarkEnd == null) return;

                // 清除书签范围内的旧内容
                var elementsBetween = GetElementsBetween(bookmark, bookmarkEnd).ToList();
                foreach (var elem in elementsBetween)
                {
                    elem.Remove();
                }

                // 创建图片 Drawing
                var drawing = CreateImageDrawing(imageId, imageName, widthEmu, heightEmu);
                var run = new Run(drawing);
                bookmark.InsertAfterSelf(run);

                doc.MainDocumentPart.Document.Save();
            }
        }

        /// <summary>
        /// 创建图片 Drawing 元素
        /// </summary>
        private static Drawing CreateImageDrawing(string imageId, string imageName,
            long widthEmu, long heightEmu)
        {
            uint imgId = (uint)(Math.Abs(imageName.GetHashCode()) % 10000);

            var pic = new PIC.Picture();
            var nvpp = new PIC.NonVisualPictureProperties();
            nvpp.Append(new PIC.NonVisualDrawingProperties { Id = imgId, Name = imageName });
            nvpp.Append(new PIC.NonVisualPictureDrawingProperties());
            pic.Append(nvpp);
            var blipFill = new PIC.BlipFill();
            blipFill.Append(new A.Blip { Embed = imageId });
            blipFill.Append(new A.Stretch(new A.FillRectangle()));
            pic.Append(blipFill);
            var spPr = new PIC.ShapeProperties();
            var xfrm = new A.Transform2D();
            xfrm.Append(new A.Offset { X = 0L, Y = 0L });
            xfrm.Append(new A.Extents { Cx = widthEmu, Cy = heightEmu });
            spPr.Append(xfrm);
            var presetGeom = new A.PresetGeometry(new A.AdjustValueList());
            presetGeom.Preset = A.ShapeTypeValues.Rectangle;
            spPr.Append(presetGeom);
            pic.Append(spPr);

            var graphicData = new A.GraphicData();
            graphicData.Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture";
            graphicData.Append(pic);

            var graphic = new A.Graphic();
            graphic.Append(graphicData);

            var inline = new DW.Inline();
            inline.DistanceFromTop = 0U;
            inline.DistanceFromBottom = 0U;
            inline.DistanceFromLeft = 0U;
            inline.DistanceFromRight = 0U;
            inline.EditId = "50D07946";
            inline.Append(new DW.Extent { Cx = widthEmu, Cy = heightEmu });
            inline.Append(new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L });
            inline.Append(new DW.DocProperties { Id = imgId, Name = imageName });
            inline.Append(new DW.NonVisualGraphicFrameDrawingProperties(
                new A.GraphicFrameLocks { NoChangeAspect = true }));
            inline.Append(graphic);

            var drawing = new Drawing();
            drawing.Append(inline);
            return drawing;
        }

        /// <summary>
        /// 对特定表格插入新行
        /// </summary>
        public void AddRowToTable(Table table)
        {
            if (table == null) return;

            // 获取最后一行作为模板
            var lastRow = table.Elements<TableRow>().LastOrDefault();
            if (lastRow == null) return;

            // 克隆新行（深拷贝）
            var newRow = (TableRow)lastRow.CloneNode(true);

            table.Append(newRow);

            // 清空新行中的内容（保留格式）
            foreach (var cell in newRow.Elements<TableCell>())
            {
                ClearCellContent(cell);
            }
        }

        /// <summary>
        /// 清空单元格内容（保留段落结构）
        /// </summary>
        private void ClearCellContent(TableCell cell)
        {
            // 获取单元格内的所有段落
            var paragraphs = cell.Elements<Paragraph>().ToList();

            foreach (var para in paragraphs)
            {
                // 删除段落中的所有Run（保留段落属性）
                var runs = para.Elements<Run>().ToList();
                foreach (var run in runs)
                {
                    run.Remove();
                }

                // 如果段落完全为空，添加一个空Run保持结构
                if (!para.HasChildren)
                {
                    para.Append(new Run(new Text("")));
                }
            }
        }

        /// <summary>
        /// 定位表格（支持书签、内容匹配、索引等多种策略）
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public Table LocateTable(WordprocessingDocument doc, string identifier)
        {
            // 策略1：先尝试书签
            var table = GetTableByBookmark(doc, identifier);
            if (table != null) return table;

            // 策略2：尝试内容匹配
            table = GetTableByContent(doc, identifier);
            if (table != null) return table;

            // 策略3：尝试索引（如果identifier是数字）
            if (int.TryParse(identifier, out int index))
            {
                table = GetTableByIndex(doc, index);
                if (table != null) return table;
            }

            return null;
        }

        /// <summary>
        /// 通过索引获取表格
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        Table GetTableByIndex(WordprocessingDocument doc, int index)
        {
            var tables = doc.MainDocumentPart.Document.Body.Elements<Table>().ToList();

            if (index < 0 || index >= tables.Count)
                return null;

            return tables[index];
        }

        /// <summary>
        /// 通过书签获取表格
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="bookmarkName"></param>
        /// <returns></returns>
        Table GetTableByBookmark(WordprocessingDocument doc, string bookmarkName)
        {
            var bookmark = doc.MainDocumentPart.Document.Body
                .Descendants<BookmarkStart>()
                .FirstOrDefault(b => b.Name == bookmarkName);

            if (bookmark == null) return null;

            // 向上查找祖先中的 Table
            return bookmark.Ancestors<Table>().FirstOrDefault();
        }

        /// <summary>
        /// 通过内容获取表格
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="searchText"></param>
        /// <returns></returns>
        Table GetTableByContent(WordprocessingDocument doc, string searchText)
        {
            return doc.MainDocumentPart.Document.Body.Elements<Table>()
                .FirstOrDefault(t => t.InnerText.Contains(searchText));
        }

        /// <summary>
        /// 在文档中插入新表格（在指定书签段落后）
        /// </summary>
        /// <param name="doc">WordprocessingDocument</param>
        /// <param name="columns">列数</param>
        /// <param name="rows">行数</param>
        /// <param name="paragraphBookmark">书签名，表格插入到该书签所在段落后。为空则添加到body末尾</param>
        /// <returns>新创建的Table</returns>
        public Table AddNewTable(WordprocessingDocument doc, int columns, int rows,
            string? paragraphBookmark = null)
        {
            if (columns < 1 || rows < 1) return null!;

            var table = new Table();

            // 设置表格属性（边框、宽度）
            var tblPr = new TableProperties(
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4 },
                    new BottomBorder { Val = BorderValues.Single, Size = 4 },
                    new LeftBorder { Val = BorderValues.Single, Size = 4 },
                    new RightBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
                ),
                new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" }
            );
            table.Append(tblPr);

            // 创建表格网格（列定义）
            var tblGrid = new TableGrid();
            for (int i = 0; i < columns; i++)
            {
                tblGrid.Append(new GridColumn());
            }
            table.Append(tblGrid);

            // 创建行
            for (int r = 0; r < rows; r++)
            {
                var tableRow = new TableRow();
                for (int c = 0; c < columns; c++)
                {
                    var tableCell = new TableCell(
                        new Paragraph(new Run(new Text("")))
                    );
                    tableRow.Append(tableCell);
                }
                table.Append(tableRow);
            }

            // 插入位置
            if (!string.IsNullOrEmpty(paragraphBookmark))
            {
                var bookmark = doc.MainDocumentPart!.Document.Body
                    .Descendants<BookmarkStart>()
                    .FirstOrDefault(b => b.Name == paragraphBookmark);

                if (bookmark != null)
                {
                    var para = bookmark.Ancestors<Paragraph>().FirstOrDefault();
                    para?.InsertAfterSelf(table);
                }
                else
                {
                    doc.MainDocumentPart!.Document.Body.Append(table);
                }
            }
            else
            {
                doc.MainDocumentPart!.Document.Body.Append(table);
            }

            return table;
        }

        /// <summary>
        /// 删除表格中的指定行（至少保留一行）
        /// </summary>
        /// <param name="table">目标表格</param>
        /// <param name="rowIndex">要删除的行索引（0-based）</param>
        public void RemoveRow(Table table, int rowIndex)
        {
            if (table == null) return;

            var rows = table.Elements<TableRow>().ToList();
            if (rows.Count <= 1) return; // 至少保留一行
            if (rowIndex < 0 || rowIndex >= rows.Count) return;

            rows[rowIndex].Remove();
        }


        //换页规则

        //表格合并规则

        //键入空白行


    }
}
