using System.Security.Cryptography;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine
{
    /// <summary>
    /// docx 正文合并器：把第 2..N 份的正文依次并进第 1 份，各起一页。
    /// </summary>
    /// <remarks>
    /// **手工正文合并，不用 altChunk。** altChunk 写法极简，但失败模式是**静默的**
    /// ——渲染器不认就整段不显示、还不报错；而本模块的预览是 OnlyOffice，查不到它对 altChunk 的支持结论。
    /// 这里产出的是纯 OOXML，不依赖任何渲染器特性；代价是代码量大，好处是**坏了会响**。
    /// **要处理的六件事**（缺一件就渲染错或保存失败）：
    /// <list type="number">
    /// <item>搬正文 —— 第 2 份 Body 的子元素克隆进第 1 份 Body 末尾（SectionProperties 之前）；</item>
    /// <item>分页 —— 靠第 2 份自己那个 inline sectPr 写显式 w:type=nextPage；</item>
    /// <item>搬页眉页脚并重映射 r:id —— ReplaceText **会写页眉页脚**，两份各有各的；</item>
    /// <item>搬图片并重映射 r:embed —— InsertMicroscopeImages 给每份都插了图；</item>
    /// <item>剥书签 —— 合并发生在渲染**之后**，书签使命已完；两份书签名完全相同，留着会重名；</item>
    /// <item>复用而非复制 styles / numbering / settings —— 同一模板产出，逐字节相同。</item>
    /// </list>
    /// 第 3、4 项是真实难度所在：部件在 SDK 里**归属于父部件**，不能跨父共享，
    /// 所以每搬一个部件都要重新 AddNewPart + FeedData + GetIdOfPart。
    /// </remarks>
    public class DocxMerger : IDocxMerger, IScopedDependency
    {
        private const string RelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        /// <inheritdoc />
        public void MergeInto(string basePath, IReadOnlyList<string> sourcePaths)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(basePath);
            ArgumentNullException.ThrowIfNull(sourcePaths);

            using var baseDoc = WordprocessingDocument.Open(basePath, true);
            var baseMain = baseDoc.MainDocumentPart
                ?? throw new InvalidOperationException("合并器: 基底文件没有 MainDocumentPart");
            var baseBody = baseMain.Document?.Body
                ?? throw new InvalidOperationException("合并器: 基底文件的正文为空");

            // 缓存键必须带**目标父部件**：media/image3.jpeg 被 footer1/2/3 三个部件共同引用，
            // 若只按源部件缓存，footer2 会拿到挂在 footer1 名下的图片部件，GetIdOfPart 直接越界。
            var partCache = new Dictionary<(OpenXmlPart Dst, OpenXmlPart Src), OpenXmlPart>();

            // 同上：外部关系也按"目标父部件"分域 —— 否则第二个页脚会命中第一个页脚的关系 id，
            // 留下悬空 r:id（OOXML 校验会逐条报 "relationship 'rIdN' does not exist"）。
            var extCache = new Dictionary<(OpenXmlPart Dst, string Type, string Uri), string>();

            // wp:docPr/@id 要求全文档唯一；两份产物的显微图 id 都是 1，克隆前必须先重编号。
            int nextDrawingId = NextDrawingId(baseMain);

            foreach (var srcPath in sourcePaths)
            {
                using var srcDoc = WordprocessingDocument.Open(srcPath, false);
                var srcMain = srcDoc.MainDocumentPart
                    ?? throw new InvalidOperationException($"合并器: {srcPath} 没有 MainDocumentPart");
                var srcBody = srcMain.Document?.Body
                    ?? throw new InvalidOperationException($"合并器: {srcPath} 的正文为空");

                // 共享部件必须逐字节一致，否则响亮失败 —— 静默错样式比报错更糟。
                EnsureSamePart(baseMain.StyleDefinitionsPart, srcMain.StyleDefinitionsPart, "styles");
                EnsureSamePart(baseMain.NumberingDefinitionsPart, srcMain.NumberingDefinitionsPart, "numbering");
                EnsureSamePart(baseMain.DocumentSettingsPart, srcMain.DocumentSettingsPart, "settings");

                // ① 建 rId 映射：把 src 的页眉/页脚/图片搬进 base
                var idMap = CopyRelations(srcMain, baseMain, partCache, extCache);

                // ② base 的 body 级 sectPr 降级为 inline（w:type 一个字不改，保住 D1 的 continuous）
                DemoteBodySectPr(baseBody);

                var srcBodySect = srcBody.Elements<SectionProperties>().LastOrDefault()
                    ?? throw new InvalidOperationException(
                        $"合并器: {srcPath} 没有 body 级 sectPr，无法搬页眉页脚归属");
                var seen = baseBody.Descendants<SectionProperties>().ToHashSet();

                // ③ 追加 src 正文（排除其 body 级 sectPr），统一重写关系属性 + 剥书签
                foreach (var el in srcBody.ChildElements.Where(e => e != srcBodySect).ToList())
                {
                    var clone = el.CloneNode(true);
                    RemapRelAttributes(clone, idMap);
                    RenumberDrawings(clone, ref nextDrawingId);
                    StripBookmarks(clone);
                    baseBody.AppendChild(clone);
                }

                // 分页就靠这一处：src 的"第一个 inline sectPr"是描述**并入内容首节**的那个，
                // 它现在不再是自己文档的首节，必须显式写 nextPage（缺省值本来就等于 nextPage，
                // 这里是把"依赖 schema 默认"变成"写明白"）。
                var added = baseBody.Descendants<SectionProperties>().Where(s => !seen.Contains(s)).ToList();
                if (added.Count > 0) EnsureExplicitNextPage(added[0]);

                // ④ 换 body 级 sectPr：用 src 的
                var newSect = srcBodySect.CloneNode(true);
                RemapRelAttributes(newSect, idMap);
                StripBookmarks(newSect);
                baseBody.AppendChild(newSect);
            }

            // 收尾：正文 + 所有页眉 + 所有页脚，书签全剥（合并发生在渲染之后，书签使命已完）
            int stripped = 0;
            stripped += StripBookmarks(baseMain.Document);
            foreach (var h in baseMain.HeaderParts) { stripped += StripBookmarks(h.Header); h.Header.Save(); }
            foreach (var f in baseMain.FooterParts) { stripped += StripBookmarks(f.Footer); f.Footer.Save(); }

            baseMain.Document.Save();

            if (stripped == 0)
                throw new InvalidOperationException(
                    "合并器: 收尾剥离书签数为 0 —— 基底里一个书签都没有，说明传进来的不是渲染后的产物，先查清楚");
        }

        // ───────────────────────── 部件搬运 ─────────────────────────

        /// <summary>只搬页眉/页脚/图片；其余（styles/numbering/settings/theme/customXml…）复用 base 的。</summary>
        private static bool ShouldCopy(OpenXmlPart p) => p is HeaderPart or FooterPart or ImagePart;

        /// <summary>
        /// 递归把 <paramref name="src"/> 下该搬的部件复制进 <paramref name="dst"/>，返回 旧rId → 新rId 的映射。
        /// </summary>
        private static Dictionary<string, string> CopyRelations(
            OpenXmlPart src,
            OpenXmlPart dst,
            Dictionary<(OpenXmlPart Dst, OpenXmlPart Src), OpenXmlPart> partCache,
            Dictionary<(OpenXmlPart Dst, string Type, string Uri), string> extCache)
        {
            var map = new Dictionary<string, string>();

            foreach (var pair in src.Parts)
            {
                var srcChild = pair.OpenXmlPart;
                if (!ShouldCopy(srcChild)) continue;

                if (!partCache.TryGetValue((dst, srcChild), out var dstChild))
                {
                    dstChild = CreatePartLike(dst, srcChild);
                    dstChild.FeedData(srcChild.GetStream());
                    partCache[(dst, srcChild)] = dstChild;

                    // 部件内部自己还有关系（页脚引图片、页眉引图片），递归搬完再重映射
                    var innerMap = CopyRelations(srcChild, dstChild, partCache, extCache);
                    if (dstChild.RootElement is { } root && innerMap.Count > 0)
                    {
                        RemapRelAttributes(root, innerMap);
                        root.Save();
                    }
                }
                map[pair.RelationshipId] = dst.GetIdOfPart(dstChild);
            }

            foreach (var ext in src.ExternalRelationships)
            {
                var key = (dst, ext.RelationshipType, ext.Uri.ToString());
                if (!extCache.TryGetValue(key, out var newId))
                {
                    newId = dst.AddExternalRelationship(ext.RelationshipType, ext.Uri).Id;
                    extCache[key] = newId;
                }
                map[ext.Id] = newId;
            }

            // 超链接关系在 SDK 里**不属于** ExternalRelationships，是单独一类 ——
            // 漏掉它，页脚里 8 条 TÜV 外链的 r:id 会全部悬空。这是本文件最要紧的一处。
            foreach (var link in src.HyperlinkRelationships)
            {
                var key = (dst, "hyperlink", link.Uri.ToString());
                if (!extCache.TryGetValue(key, out var newId))
                {
                    newId = dst.AddHyperlinkRelationship(link.Uri, link.IsExternal).Id;
                    extCache[key] = newId;
                }
                map[link.Id] = newId;
            }

            return map;
        }

        private static OpenXmlPart CreatePartLike(OpenXmlPart dstParent, OpenXmlPart srcPart) => srcPart switch
        {
            HeaderPart => ((MainDocumentPart)dstParent).AddNewPart<HeaderPart>(),
            FooterPart => ((MainDocumentPart)dstParent).AddNewPart<FooterPart>(),
            ImagePart img => dstParent switch
            {
                MainDocumentPart m => m.AddImagePart(ImageTypeOf(img)),
                HeaderPart h => h.AddImagePart(ImageTypeOf(img)),
                FooterPart f => f.AddImagePart(ImageTypeOf(img)),
                _ => throw new NotSupportedException($"合并器: 图片部件的父级不支持 — {dstParent.GetType().Name}")
            },
            _ => throw new NotSupportedException(
                $"合并器不支持的部件类型: {srcPart.GetType().Name} ({srcPart.ContentType})")
        };

        private static PartTypeInfo ImageTypeOf(ImagePart img) => img.ContentType switch
        {
            "image/jpeg" => ImagePartType.Jpeg,
            "image/png" => ImagePartType.Png,
            "image/gif" => ImagePartType.Gif,
            "image/bmp" => ImagePartType.Bmp,
            "image/tiff" => ImagePartType.Tiff,
            "image/x-emf" => ImagePartType.Emf,
            "image/x-wmf" => ImagePartType.Wmf,
            "image/x-pcx" => ImagePartType.Pcx,
            _ => throw new NotSupportedException($"合并器不认识的图片类型: {img.ContentType}")
        };

        // ───────────────────────── 元素改写 ─────────────────────────

        /// <summary>把所有 r: 命名空间下的关系属性值，按映射换成新 rId。</summary>
        private static void RemapRelAttributes(OpenXmlElement root, Dictionary<string, string> map)
        {
            foreach (var el in SelfAndDescendants(root))
            {
                var attrs = el.GetAttributes().Where(a => a.NamespaceUri == RelNs).ToList();
                foreach (var attr in attrs)
                    if (map.TryGetValue(attr.Value!, out var newId))
                        el.SetAttribute(new OpenXmlAttribute(attr.Prefix, attr.LocalName, attr.NamespaceUri, newId));
            }
        }

        private static IEnumerable<OpenXmlElement> SelfAndDescendants(OpenXmlElement root)
        {
            yield return root;
            foreach (var d in root.Descendants()) yield return d;
        }

        /// <summary>正文、页眉、页脚三个范围一起取最大，保证新号段与全文档都不撞。</summary>
        private static int NextDrawingId(MainDocumentPart main)
        {
            var parts = new List<OpenXmlPart> { main };
            parts.AddRange(main.HeaderParts);
            parts.AddRange(main.FooterParts);

            return parts
                .SelectMany(p => p.RootElement?.Descendants<DW.DocProperties>() ?? Enumerable.Empty<DW.DocProperties>())
                .Select(d => (int)(d.Id?.Value ?? 0u))
                .DefaultIfEmpty(0)
                .Max() + 1;
        }

        /// <summary>给克隆进来的 drawing 重新编号：wp:docPr 与配对的 pic:cNvPr 同步，保证全文档唯一。</summary>
        private static void RenumberDrawings(OpenXmlElement root, ref int nextId)
        {
            foreach (var draw in root.Descendants<Drawing>())
            {
                var docPr = draw.Descendants<DW.DocProperties>().FirstOrDefault();
                if (docPr == null) continue;

                var id = (uint)nextId++;
                docPr.Id = id;

                var cNvPr = draw.Descendants<PIC.NonVisualDrawingProperties>().FirstOrDefault();
                if (cNvPr != null) cNvPr.Id = id;
            }
        }

        private static int StripBookmarks(OpenXmlElement? root)
        {
            if (root == null) return 0;

            var starts = root.Descendants<BookmarkStart>().ToList();
            var ends = root.Descendants<BookmarkEnd>().ToList();
            foreach (var b in starts) b.Remove();
            foreach (var b in ends) b.Remove();

            return starts.Count + ends.Count;
        }

        /// <summary>
        /// 把 body 级 sectPr 降级成最后一个段落里的 inline sectPr。
        /// </summary>
        /// <remarks>
        /// w:type **一个字不改** —— 这个 sectPr 描述的仍是它原来那一节，
        /// 改它会把 D1 刻意保留的 continuous 弄丢。
        /// </remarks>
        private static void DemoteBodySectPr(Body body)
        {
            var sect = body.Elements<SectionProperties>().LastOrDefault();
            if (sect == null) return;

            var prev = sect.PreviousSibling();
            sect.Remove();

            if (prev is Paragraph p)
            {
                p.ParagraphProperties ??= new ParagraphProperties();

                // ParagraphPropertiesChange 必须是 pPr 的最后一个子元素，插在它之前
                var change = p.ParagraphProperties.Elements<ParagraphPropertiesChange>().FirstOrDefault();
                if (change != null) p.ParagraphProperties.InsertBefore(sect, change);
                else p.ParagraphProperties.AppendChild(sect);
            }
            else
            {
                body.AppendChild(new Paragraph(new ParagraphProperties(sect)));
            }
        }

        /// <summary>把 w:type 显式写成 nextPage。</summary>
        private static void EnsureExplicitNextPage(SectionProperties sect)
        {
            var t = sect.Elements<SectionType>().FirstOrDefault();
            if (t != null) { t.Val = SectionMarkValues.NextPage; return; }

            var st = new SectionType { Val = SectionMarkValues.NextPage };

            // CT_SectPr 的序列是 headerReference*/footerReference*/footnotePr/endnotePr/type/…，
            // type 必须排在那些引用之后，否则 schema 不认。
            var last = sect.Elements().LastOrDefault(e =>
                e is HeaderReference or FooterReference or FootnoteProperties or EndnoteProperties);

            if (last != null) sect.InsertAfter(st, last);
            else sect.PrependChild(st);
        }

        /// <summary>共享部件必须逐字节一致，否则合并出来的样式会自相矛盾。</summary>
        private static void EnsureSamePart(OpenXmlPart? a, OpenXmlPart? b, string name)
        {
            if (a == null && b == null) return;
            if (a == null || b == null)
                throw new InvalidOperationException($"合并器: {name} 部件只在一侧存在，不支持合并");

            using var sa = a.GetStream();
            using var sb = b.GetStream();
            if (!SHA256.HashData(sa).SequenceEqual(SHA256.HashData(sb)))
                throw new InvalidOperationException($"合并器: {name} 部件在两侧不一致，不支持合并");
        }
    }
}
