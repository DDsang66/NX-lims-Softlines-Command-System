using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Application.Interface.FiberTeamContext;
using NX_lims_Softlines_Command_System.src.Domain.Aggregeates.FiberContext.IngredientAnalysis;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine
{
    /// <summary>
    /// Word 模板引擎
    /// 仅封装底层操作功能，不涉及业务逻辑
    /// </summary>
    public class WordTemplateEngine : IWordTemplateEngine, IScopedDependency
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
        /// <param name="redBookmarks">标红的书签名</param>
        /// <param name="removeWhenEmpty">值为空时**删掉书签前的文字**（标签类的书签用；所在表格保留）</param>
        /// <param name="removeBlockWhenEmpty">
        /// 值为空时**把书签所在的整张表删掉**（整块类的书签用，如 Conclusion / Remark）。
        /// 与 <paramref name="removeWhenEmpty"/> 是两件事，别合并 —— 后者的典型用户 Recommend
        /// 就住在结果表里，那张表**删不得**。见 <see cref="RemoveTableForBookmark"/>。
        /// </param>
        /// <param name="replaceParagraphText">
        /// 整段文字改写表（段落的完整文字 → 新文字）。给模板里那些**连书签都没有的固定标签**
        /// 用 —— 它们换不了"值"，只能整段换掉。传 null（缺省）则一个段落都不动。
        /// 见 <see cref="ReplaceParagraphTexts"/>。
        /// </param>
        public void ReplaceText(string filePath, Dictionary<string, string> bookmarkValues, HashSet<string>? redBookmarks = null, HashSet<string>? removeWhenEmpty = null, HashSet<string>? removeBlockWhenEmpty = null, IReadOnlyDictionary<string, string>? replaceParagraphText = null)
        {
            if (bookmarkValues == null || !bookmarkValues.Any()) return;

            using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, true))
            {
                // 整段改写**必须**排在书签处理之前：Recommendation 的标签与 Recommend 书签同段，
                // 推荐标签为空时 RemoveTextBeforeBookmark 会把标签一起删掉 —— 先改后删，
                // 与"英文标签跟着消失"的既有行为逐字一致；反过来先删改不到，就会留下一行中文空标签。
                if (replaceParagraphText != null && replaceParagraphText.Count > 0)
                {
                    ReplaceParagraphTexts(doc.MainDocumentPart!, replaceParagraphText);
                    doc.MainDocumentPart.Document!.Save();

                    foreach (var headerPart in doc.MainDocumentPart.HeaderParts)
                    {
                        ReplaceParagraphTexts(headerPart, replaceParagraphText);
                        headerPart.Header?.Save();
                    }

                    foreach (var footerPart in doc.MainDocumentPart.FooterParts)
                    {
                        ReplaceParagraphTexts(footerPart, replaceParagraphText);
                        footerPart.Footer?.Save();
                    }
                }

                // 正文部件
                ReplaceBookmarksInPart(doc.MainDocumentPart!, bookmarkValues, redBookmarks, removeWhenEmpty, removeBlockWhenEmpty);
                doc.MainDocumentPart.Document!.Save();

                // 页眉
                foreach (var headerPart in doc.MainDocumentPart!.HeaderParts)
                {
                    ReplaceBookmarksInPart(headerPart, bookmarkValues, redBookmarks, removeWhenEmpty, removeBlockWhenEmpty);
                    headerPart.Header?.Save();
                }

                // 页脚
                foreach (var footerPart in doc.MainDocumentPart.FooterParts)
                {
                    ReplaceBookmarksInPart(footerPart, bookmarkValues, redBookmarks, removeWhenEmpty, removeBlockWhenEmpty);
                    footerPart.Footer?.Save();
                }
            }
        }

        /// <summary>
        /// 按**整段文字**改写固定标签。给那些在模板里是纯文本、连书签都没有的常量用
        /// （成分报告上的 `Test Result:` / `Recommendation:` 等五个）。
        /// </summary>
        /// <remarks>
        /// **匹配的是"段内全部文字串起来后 trim"**，不是"某个 `<w:t>` 等于" ——
        /// 同一个标签在模板里可能被拆成好几个 run（`Based on moisture regain weight:`
        /// 就是 `"Based on "` / `"moisture"` / `" regain weight:"` 三个），
        /// 只比单个 `<w:t>` 永远匹配不上。
        ///
        /// 改写**就地**进行：第一个 `<w:t>` 换成新文字，同段其余 `<w:t>` 清空。
        /// 样式（粗体、字号、对齐）挂在 `<w:rPr>` 上、不在 `<w:t>` 上，保留原 Run 即保留版式；
        /// 而**清空 Text 而不是删 Run**，是因为同段还可能有书签、域、制表位等非文字元素，
        /// 删 Run 会把它们一并带走。
        /// </remarks>
        private static void ReplaceParagraphTexts(OpenXmlPart part, IReadOnlyDictionary<string, string> replaceParagraphText)
        {
            var paragraphs = part.RootElement?.Descendants<Paragraph>().ToList();
            if (paragraphs == null) return;

            foreach (var paragraph in paragraphs)
            {
                // 段内所有 <w:t>
                var texts = paragraph.Descendants<Text>().ToList();
                if (texts.Count == 0) continue;

                // 串起来
                var paragraphText = string.Concat(texts.Select(t => t.Text));
                if (!replaceParagraphText.TryGetValue(paragraphText.Trim(), out var replacement)) continue;

                // 就地改第一个
                texts[0].Text = replacement;
                texts[0].Space = SpaceProcessingModeValues.Preserve;

                // 其余清空
                for (int i = 1; i < texts.Count; i++)
                    texts[i].Text = string.Empty;
            }
        }

        /// <summary>
        /// 在指定部件中替换书签
        /// 优先在原有 Run/Text 上就地替换以保留样式；若不存在则寻找局部最近的 RunProperties 并克隆；最后才插入无样式 Run。
        /// </summary>
        private void ReplaceBookmarksInPart(OpenXmlPart part, Dictionary<string, string> bookmarkValues, HashSet<string>? redBookmarks, HashSet<string>? removeWhenEmpty, HashSet<string>? removeBlockWhenEmpty)
        {
            var bookmarks = part.RootElement!.Descendants<BookmarkStart>()
                .Where(b => bookmarkValues.ContainsKey(b.Name!))
                .ToList();

            foreach (var bookmark in bookmarks)
            {
                // 找到对应的 BookmarkEnd
                var bookmarkEnd = part.RootElement.Descendants<BookmarkEnd>()
                    .FirstOrDefault(be => be.Id?.Value == bookmark.Id?.Value);

                if (bookmarkEnd == null) continue;

                // 整块删：书签值为空且被点名 → 把书签所在的**整张表**拿掉（Conclusion / Remark 这类整块）。
                // 必须 continue —— 表都摘掉了，后面的"就地替换 / 找最近 RunProperties"就没有意义了。
                if (removeBlockWhenEmpty != null && removeBlockWhenEmpty.Contains(bookmark.Name!)
                    && string.IsNullOrEmpty(bookmarkValues[bookmark.Name]))
                {
                    RemoveTableForBookmark(bookmark);
                    continue;
                }

                // 若书签在 removeWhenEmpty 中且替换值为空，删除书签前的文本
                if (removeWhenEmpty != null && removeWhenEmpty.Contains(bookmark.Name!)
                    && string.IsNullOrEmpty(bookmarkValues[bookmark.Name]))
                {
                    RemoveTextBeforeBookmark(bookmark);
                }

                // 获取书签之间的所有元素（同一父级序列）
                var contentElements = GetContentBetweenBookmarks(part, bookmark, bookmarkEnd);

                // 优先在原有 Run 的 Text 上就地替换（保留 RunProperties）
                var existingRunWithText = contentElements.OfType<Run>()
                    .FirstOrDefault(r => r.Elements<Text>().Any());

                if (existingRunWithText != null)
                {
                    // 清空已有 text 元素，用 InsertTextWithLineBreaks 写入（支持 \n 换行）
                    foreach (var t in existingRunWithText.Elements<Text>().ToList())
                        t.Remove();
                    TextRunHelper.InsertTextWithLineBreaks(bookmarkValues[bookmark.Name], existingRunWithText);

                    // 标红：检查是否在 redBookmarks 中
                    if (redBookmarks != null && redBookmarks.Contains(bookmark.Name))
                        ApplyRedColor(existingRunWithText);

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
                    var newRun = new Run(nearestRunProps.CloneNode(true) as RunProperties);
                    InsertRunAfterBookmark(bookmark, newRun);
                    TextRunHelper.InsertTextWithLineBreaks(bookmarkValues[bookmark.Name], newRun);
                    if (redBookmarks != null && redBookmarks.Contains(bookmark.Name))
                        ApplyRedColor(newRun);
                }
                else
                {
                    // 兜底：插入无样式的 Run（将使用 Word 的默认样式）
                    var newRun = new Run();
                    InsertRunAfterBookmark(bookmark, newRun);
                    TextRunHelper.InsertTextWithLineBreaks(bookmarkValues[bookmark.Name], newRun);
                    if (redBookmarks != null && redBookmarks.Contains(bookmark.Name))
                        ApplyRedColor(newRun);
                }
            }
        }

        /// <summary>
        /// 将 Run 的字体颜色设为红色（FF0000）
        /// </summary>
        private static void ApplyRedColor(Run run)
        {
            run.RunProperties ??= new RunProperties();
            var color = run.RunProperties.Elements<Color>().FirstOrDefault();
            if (color != null)
                color.Val = "FF0000";
            else
                run.RunProperties.Append(new Color { Val = "FF0000" });
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
        /// 删除书签前（同一父元素内）的文本 — 用于 removeWhenEmpty
        /// </summary>
        private void RemoveTextBeforeBookmark(BookmarkStart bookmark)
        {
            var parent = bookmark.Parent;
            if (parent == null) return;

            // 收集 BookmarkStart 之前的所有 Text 元素
            var elementsBefore = parent.ChildElements
                .TakeWhile(e => e != bookmark)
                .OfType<Run>()
                .SelectMany(r => r.Elements<Text>())
                .ToList();

            foreach (var text in elementsBefore)
                text.Remove();

            // 也删除 BookmarkStart 之前独立的 Run（里面可能只有 Text，已被上面清空）
            var runsBefore = parent.ChildElements
                .TakeWhile(e => e != bookmark)
                .OfType<Run>()
                .Where(r => !r.Elements<Text>().Any())
                .ToList();

            foreach (var run in runsBefore)
                run.Remove();
        }

        /// <summary>
        /// 把书签所在的**整张表**删掉 —— 用于 removeBlockWhenEmpty。
        ///
        /// <para>
        /// 为什么非删表不可：Conclusion / Remark 这两块在模板里就是两张**无边框**的表
        /// （`tblBorders` 全 none），标签 "Conclusion:" / "Remark:" 是表里的**普通文字**，
        /// 不是书签。所以只把书签值清空、或者只按 <see cref="RemoveTextBeforeBookmark"/>
        /// 删掉标签，都会剩下一张空表占着版面 —— 看起来就是报告里凭空多一块空白。
        /// </para>
        ///
        /// <para>
        /// 连带的空段落：两张表前后各有一个 9pt 的空段落当间距。表没了、段落还留着，
        /// 就是 ~18pt 的空白。所以表被删掉时，把它**后面**紧邻的空段落一起收掉，
        /// 收到最后一个非空段落为止。判据见 <see cref="IsTrulyEmpty"/>（从严）。
        /// </para>
        ///
        /// <para>
        /// ⚠️ 收空段落**只在表前面也是空段落时才做**：这样保证收完至少还剩一个空段落，
        /// 相邻两张表不会因此贴到一起 —— Word 会把紧挨着的两张表合并成一张，那是另一个 bug。
        /// </para>
        /// </summary>
        private void RemoveTableForBookmark(BookmarkStart bookmark)
        {
            var table = bookmark.Ancestors<Table>().FirstOrDefault();
            if (table == null) return;   // 书签不在表里：什么都不做，绝不误删正文

            if (table.PreviousSibling() is Paragraph prev && IsTrulyEmpty(prev))
            {
                var next = table.NextSibling();
                while (next is Paragraph p && IsTrulyEmpty(p))
                {
                    var after = p.NextSibling();
                    p.Remove();
                    next = after;
                }
            }

            table.Remove();
        }

        /// <summary>
        /// 真空段落：没有非空白文字、没有书签、没有图/对象/换行/分页。
        /// 判据从严 —— 拿不准就当作"非空"，宁可不收段落，也不误删内容。
        /// </summary>
        private static bool IsTrulyEmpty(Paragraph p)
            => !p.Descendants<Text>().Any(t => !string.IsNullOrWhiteSpace(t.Text))
            && !p.Descendants<BookmarkStart>().Any()
            && !p.Descendants().Any(e => e.LocalName is "drawing" or "pict" or "object" or "br");

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
        }

        /// <summary>
        /// 对word插入新表（预留）
        /// </summary>
        /// <param name="doc">WordprocessingDocument</param>
        /// <param name="columns">列数</param>
        /// <param name="rows">行数</param>
        /// <param name="paragraphBookmark">书签名，表格插入到该书签所在段落后。为空则添加到body末尾</param>
        /// <returns>新创建的Table</returns>
        public Table AddNewTable(WordprocessingDocument doc, int columns, int rows,
            string? paragraphBookmark = null)
        {
            return null!;
        }

        /// <summary>
        /// 删除表格中的某一行（预留）
        /// </summary>
        /// <param name="table">目标表格</param>
        /// <param name="rowIndex">要删除的行索引（0-based）</param>
        public void RemoveRow(Table table, int rowIndex)
        {
            // 可能需要触发同一表格之中书签顺序的更新
        }

        /// <summary>
        /// 在 Microscopeview1~14 书签后按输入纤维顺序填入对应图片
        /// </summary>
        public void InsertMicroscopeImages(string filePath, IEnumerable<string> fiberNames, string imageFolder)
        {
            using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, true))
            {
                var mainPart = doc.MainDocumentPart!;
                var body = mainPart.Document.Body;

                var bookmarks = body.Descendants<BookmarkStart>()
                    .Where(b => b.Name != null && b.Name.Value != null && b.Name.Value.StartsWith("Microscopeview"))
                    .OrderBy(b => int.Parse(b.Name!.Value!.Replace("Microscopeview", "")))
                    .ToList();

                int slotIndex = 0;

                foreach (var fiberName in fiberNames)
                {
                    if (string.IsNullOrWhiteSpace(fiberName)) continue;

                    // 图库文件名走别名表（Spandex→Elastane 等，见 MicroscopeImageName）。
                    // 只用别名**找文件** —— 下面图注那行仍用录入名 fiberName，一字未改。
                    var imagePath = Path.Combine(imageFolder, $"{MicroscopeImageName.Resolve(fiberName)}.png");
                    if (!File.Exists(imagePath)) continue;

                    if (slotIndex >= bookmarks.Count) break;

                    var bookmark = bookmarks[slotIndex++];

                    byte[] imageBytes = File.ReadAllBytes(imagePath);
                    var mediaPart = mainPart.AddImagePart(ImagePartType.Png);
                    using var ms = new MemoryStream(imageBytes);
                    mediaPart.FeedData(ms);
                    string contentId = mainPart.GetIdOfPart(mediaPart);

                    var drawing = CreateImageDrawing(contentId, fiberName, (uint)slotIndex);

                    var paragraph = bookmark.Ancestors<Paragraph>().FirstOrDefault();
                    if (paragraph == null) continue;

                    // 获取书签段的 RunProperties 用于文字样式
                    var refRunProps = paragraph.Elements<Run>()
                        .Select(r => r.RunProperties)
                        .FirstOrDefault(rp => rp != null);

                    // 在书签所在段落后插入图片段+名称段
                    var imageParagraph = new Paragraph(
                        new ParagraphProperties(
                            new SpacingBetweenLines { After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto }
                        ),
                        new Run(
                            new RunProperties(new NoProof()),
                            drawing
                        )
                    );
                    var nameParagraph = new Paragraph(
                        new ParagraphProperties(
                            new SpacingBetweenLines { After = "0", Line = "240", LineRule = LineSpacingRuleValues.Auto }
                        ),
                        new Run(
                            refRunProps?.CloneNode(true) as RunProperties ?? new RunProperties(),
                            new Text(fiberName) { Space = SpaceProcessingModeValues.Preserve }
                        )
                    );
                    paragraph.Parent!.InsertAfter(nameParagraph, paragraph);
                    paragraph.Parent!.InsertAfter(imageParagraph, paragraph);
                }

                mainPart.Document.Save();
            }
        }

        private static Drawing CreateImageDrawing(string relationshipId, string imageName, uint id)
        {
            const long emuCm = 360000;
            long width = 2 * emuCm;
            long height = 135 * emuCm / 100;  // 1.35cm

            return new Drawing(
                new DW.Inline(
                    new DW.Extent { Cx = width, Cy = height },
                    new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                    new DW.DocProperties { Id = id, Name = imageName },
                    new DW.NonVisualGraphicFrameDrawingProperties(
                        new A.GraphicFrameLocks { NoChangeAspect = true }
                    ),
                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties { Id = id, Name = $"{imageName}.png" },
                                    new PIC.NonVisualPictureDrawingProperties()
                                ),
                                new PIC.BlipFill(
                                    new A.Blip { Embed = relationshipId },
                                    new A.Stretch(new A.FillRectangle())
                                ),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset { X = 0L, Y = 0L },
                                        new A.Extents { Cx = width, Cy = height }
                                    ),
                                    new A.PresetGeometry(new A.AdjustValueList())
                                    { Preset = A.ShapeTypeValues.Rectangle }
                                )
                            )
                        )
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                    )
                )
                {
                    DistanceFromTop = 0, DistanceFromBottom = 0,
                    DistanceFromLeft = 0, DistanceFromRight = 0
                }
            );
        }

        // 换页规则
        // 表格合并规则
        // 键入空白行
    }
}