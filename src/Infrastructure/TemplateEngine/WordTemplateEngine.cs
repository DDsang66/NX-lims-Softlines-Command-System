using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
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
        /// 图片替换书签位
        /// </summary>
        public void ReplaceWithImage()
        {

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
        /// 对word插入新表
        /// </summary>
        public void AddNewTable()
        {

        }

        /// <summary>
        /// 删除表格中的某一行
        /// </summary>
        public void RemoveRow()
        {
            //可能需要触发同一表格之中书签顺序的更新
        }


        //换页规则

        //表格合并规则

        //键入空白行


    }
}
