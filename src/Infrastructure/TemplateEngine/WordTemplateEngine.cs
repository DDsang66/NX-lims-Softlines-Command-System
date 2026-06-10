using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine
{
    /// <summary>
    /// Word 模板引擎
    /// 仅封装底层操作功能，不涉及业务逻辑
    /// </summary>
    public class WordTemplateEngine : IScopedDependency
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
            if (bookmarkValues == null || !bookmarkValues.Any()) return;

            using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, true))
            {
                // 正文部件
                ReplaceBookmarksInPart(doc.MainDocumentPart!, bookmarkValues);
                doc.MainDocumentPart.Document!.Save();

                // 页眉
                foreach (var headerPart in doc.MainDocumentPart!.HeaderParts)
                {
                    ReplaceBookmarksInPart(headerPart, bookmarkValues);
                    headerPart.Header?.Save();
                }

                // 页脚
                foreach (var footerPart in doc.MainDocumentPart.FooterParts)
                {
                    ReplaceBookmarksInPart(footerPart, bookmarkValues);
                    footerPart.Footer?.Save();
                }
            }
        }

        /// <summary>
        /// 在指定部件中替换书签
        /// 优先在原有 Run/Text 上就地替换以保留样式；若不存在则寻找局部最近的 RunProperties 并克隆；最后才插入无样式 Run。
        /// </summary>
        private void ReplaceBookmarksInPart(OpenXmlPart part, Dictionary<string, string> bookmarkValues)
        {
            var bookmarks = part.RootElement!.Descendants<BookmarkStart>()
                .Where(b => bookmarkValues.ContainsKey(b.Name!))
                .ToList();

            foreach (var bookmark in bookmarks)
            {
                // 找到对应的 BookmarkEnd
                var bookmarkEnd = part.RootElement.Descendants<BookmarkEnd>()
                    .FirstOrDefault(be => be.Id == bookmark.Id);

                if (bookmarkEnd == null) continue;

                // 获取书签之间的所有元素（同一父级序列）
                var contentElements = GetContentBetweenBookmarks(part, bookmark, bookmarkEnd);

                // 优先在原有 Run 的 Text 上就地替换（保留 RunProperties）
                var existingRunWithText = contentElements.OfType<Run>()
                    .FirstOrDefault(r => r.Elements<Text>().Any());

                if (existingRunWithText != null)
                {
                    var textElem = existingRunWithText.Elements<Text>().First();
                    textElem.Text = bookmarkValues[bookmark.Name];
                    textElem.Space = SpaceProcessingModeValues.Preserve;

                    // 删除书签范围内除保留的 run 之外的其他元素
                    foreach (var elem in contentElements.ToList())
                    {
                        if (!object.ReferenceEquals(elem, existingRunWithText))
                        {
                            elem.Remove();
                        }
                    }

                    continue;
                }

                // 如果没有就地可替换的 Run/Text，尝试寻找最近的 RunProperties（段落优先，单元格次之）
                var nearestRunProps = FindNearestRunProperties(bookmark);

                if (nearestRunProps != null)
                {
                    var newRun = new Run(nearestRunProps.CloneNode(true) as RunProperties,
                                         new Text(bookmarkValues[bookmark.Name]) { Space = SpaceProcessingModeValues.Preserve });
                    InsertRunAfterBookmark(bookmark, newRun);
                }
                else
                {
                    // 兜底：插入无样式的 Run（将使用 Word 的默认样式）
                    var newRun = new Run(new Text(bookmarkValues[bookmark.Name]) { Space = SpaceProcessingModeValues.Preserve });
                    InsertRunAfterBookmark(bookmark, newRun);
                }
            }
        }

        /// <summary>
        /// 在书签位置后插入 Run，确保插入点有效（书签 Parent 可能是 Run、Paragraph 等）
        /// </summary>
        private void InsertRunAfterBookmark(BookmarkStart bookmark, Run run)
        {
            OpenXmlElement? parent = bookmark.Parent;
            if (parent == null)
            {
                // 兜底：将 run 插入到书签的祖先段落末尾
                var para = bookmark.Ancestors<Paragraph>().FirstOrDefault();
                if (para != null) para.Append(run);
                return;
            }

            try
            {
                parent.InsertAfter(run, bookmark);
            }
            catch
            {
                // 若直接插入失败，退回到父段落末尾
                var para = bookmark.Ancestors<Paragraph>().FirstOrDefault();
                if (para != null) para.Append(run);
            }
        }

        /// <summary>
        /// 在书签与对应 BookmarkEnd 之间收集元素（基于 NextSibling 遍历，适用于位于同一父级的情况）
        /// </summary>
        private List<OpenXmlElement> GetContentBetweenBookmarks(OpenXmlPart part, BookmarkStart bookmark, BookmarkEnd bookmarkEnd)
        {
            var result = new List<OpenXmlElement>();
            var current = bookmark.NextSibling();

            while (current != null && current != bookmarkEnd)
            {
                result.Add(current);
                current = current.NextSibling();
            }

            return result;
        }

        /// <summary>
        /// 清除书签之间的内容（保留书签标记）
        /// </summary>
        private void ClearContentBetweenBookmarks(OpenXmlPart part, BookmarkStart bookmark, BookmarkEnd bookmarkEnd)
        {
            var current = bookmark.NextSibling();

            while (current != null && current != bookmarkEnd)
            {
                var next = current.NextSibling();
                current.Remove();
                current = next;
            }
        }

        /// <summary>
        /// 查找书签附近最近的 RunProperties：优先同段落相邻 Run，若没有则在同单元格内查找。
        /// 不再退回到部件任意 Run，以避免把不同位置的样式统一。
        /// </summary>
        private RunProperties? FindNearestRunProperties(BookmarkStart bookmark)
        {
            // 1. 同段落内查找相邻 RunProperties（向前向后）
            var para = bookmark.Ancestors<Paragraph>().FirstOrDefault();
            if (para != null)
            {
                var children = para.ChildElements.ToList();
                int idx = children.IndexOf(bookmark);
                if (idx >= 0)
                {
                    for (int i = idx - 1; i >= 0; i--)
                    {
                        if (children[i] is Run r && r.RunProperties != null)
                        {
                            return r.RunProperties.CloneNode(true) as RunProperties;
                        }
                    }

                    for (int i = idx + 1; i < children.Count; i++)
                    {
                        if (children[i] is Run r && r.RunProperties != null)
                        {
                            return r.RunProperties.CloneNode(true) as RunProperties;
                        }
                    }
                }
            }

            // 2. 在同一 TableCell 范围内查找任一带格式的 Run
            var cell = bookmark.Ancestors<TableCell>().FirstOrDefault();
            if (cell != null)
            {
                var runInCell = cell.Descendants<Run>().FirstOrDefault(r => r.RunProperties != null);
                if (runInCell != null)
                {
                    return runInCell.RunProperties.CloneNode(true) as RunProperties;
                }
            }

            return null;
        }

        /// <summary>
        /// 替换书签内的内容（备用方法：在同段落内删除并插入新 Run）
        /// </summary>
        private void ReplaceBookmarkContent(BookmarkStart start, BookmarkEnd end, string newText)
        {
            var parentPara = start.Ancestors<Paragraph>().FirstOrDefault();
            if (parentPara == null) return;

            var elementsBetween = GetElementsBetween(start, end).ToList();

            foreach (var elem in elementsBetween)
            {
                elem.Remove();
            }

            var newRun = new Run(
                new RunProperties(),
                new Text(newText) { Space = SpaceProcessingModeValues.Preserve }
            );

            start.InsertAfterSelf(newRun);
        }

        /// <summary>
        /// 获取两个元素之间的所有元素（仅处理在同一父级内的情况）
        /// </summary>
        private IEnumerable<OpenXmlElement> GetElementsBetween(BookmarkStart start, BookmarkEnd end)
        {
            var parent = start.Parent;
            if (parent != end.Parent) yield break;

            bool started = false;
            foreach (var elem in parent!.ChildElements.ToList())
            {
                if (elem == start)
                {
                    started = true;
                    continue;
                }

                if (elem == end) yield break;

                if (started) yield return elem;
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
        /// 图片替换书签位（预留）
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

            var lastRow = table.Elements<TableRow>().LastOrDefault();
            if (lastRow == null) return;

            var newRow = (TableRow)lastRow.CloneNode(true);

            table.Append(newRow);

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
        /// 定位表格（支持书签、内容匹配、索引等多种策略）
        /// </summary>
        public Table? LocateTable(WordprocessingDocument doc, string identifier)
        {
            var table = GetTableByBookmark(doc, identifier);
            if (table != null) return table;

            table = GetTableByContent(doc, identifier);
            if (table != null) return table;

            if (int.TryParse(identifier, out int index))
            {
                table = GetTableByIndex(doc, index);
                if (table != null) return table;
            }

            return null;
        }

        private Table? GetTableByIndex(WordprocessingDocument doc, int index)
        {
            var tables = doc.MainDocumentPart.Document.Body.Elements<Table>().ToList();

            if (index < 0 || index >= tables.Count)
                return null;

            return tables[index];
        }

        private Table? GetTableByBookmark(WordprocessingDocument doc, string bookmarkName)
        {
            var bookmark = doc.MainDocumentPart.Document.Body
                .Descendants<BookmarkStart>()
                .FirstOrDefault(b => b.Name == bookmarkName);

            if (bookmark == null) return null;

            return bookmark.Ancestors<Table>().FirstOrDefault();
        }

        private Table? GetTableByContent(WordprocessingDocument doc, string searchText)
        {
            return doc.MainDocumentPart.Document.Body.Elements<Table>()
                .FirstOrDefault(t => t.InnerText.Contains(searchText));
        }

        /// <summary>
        /// 对word插入新表（预留）
        /// </summary>
        public void AddNewTable()
        {
        }

        /// <summary>
        /// 删除表格中的某一行（预留）
        /// </summary>
        public void RemoveRow()
        {
            // 可能需要触发同一表格之中书签顺序的更新
        }

        // 换页规则
        // 表格合并规则
        // 键入空白行
    }
}