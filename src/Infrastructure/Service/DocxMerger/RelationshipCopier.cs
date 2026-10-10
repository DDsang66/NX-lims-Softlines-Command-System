using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Service.DocxMerger
{
    internal static class RelationshipCopier
    {
        // ============ 页眉/页脚引用复制 ============
        public static void CopyHeaderFooterReferences(
            MainDocumentPart sourceMain,
            MainDocumentPart targetMain,
            SectionProperties? sourceSectPr,     // ← 外面传进来，这里直接用
            SectionProperties targetSectPr,
            ILogger logger)
        {
            if (sourceSectPr == null) return;

            // 清除目标 sectPr 已有引用
            foreach (var hr in targetSectPr.Elements<HeaderReference>().ToList()) hr.Remove();
            foreach (var fr in targetSectPr.Elements<FooterReference>().ToList()) fr.Remove();

            var newHeaderRefs = new List<HeaderReference>();
            var newFooterRefs = new List<FooterReference>();

            // 页眉
            var headerTypesSeen = new HashSet<HeaderFooterValues>();
            foreach (var srcHr in sourceSectPr.Elements<HeaderReference>())
            {
                if (srcHr.Id?.Value == null) continue;
                var type = srcHr.Type?.Value ?? HeaderFooterValues.Default;
                if (!headerTypesSeen.Add(type)) continue;

                if (sourceMain.GetPartById(srcHr.Id.Value) is not HeaderPart srcPart) continue;

                var newPart = targetMain.AddNewPart<HeaderPart>();
                var newPartId = targetMain.GetIdOfPart(newPart);

                using (var s = srcPart.GetStream())
                using (var t = newPart.GetStream(FileMode.Create, FileAccess.Write))
                    s.CopyTo(t);

                CopyPartRelationships(srcPart, newPart, logger);

                newHeaderRefs.Add(new HeaderReference { Id = newPartId, Type = type });
                logger.LogDebug("复制页眉 Type={Type} -> {Id}", type, newPartId);
            }

            // 页脚
            var footerTypesSeen = new HashSet<HeaderFooterValues>();
            foreach (var srcFr in sourceSectPr.Elements<FooterReference>())
            {
                if (srcFr.Id?.Value == null) continue;
                var type = srcFr.Type?.Value ?? HeaderFooterValues.Default;
                if (!footerTypesSeen.Add(type)) continue;

                if (sourceMain.GetPartById(srcFr.Id.Value) is not FooterPart srcPart) continue;

                var newPart = targetMain.AddNewPart<FooterPart>();
                var newPartId = targetMain.GetIdOfPart(newPart);

                using (var s = srcPart.GetStream())
                using (var t = newPart.GetStream(FileMode.Create, FileAccess.Write))
                    s.CopyTo(t);

                CopyPartRelationships(srcPart, newPart, logger);

                newFooterRefs.Add(new FooterReference { Id = newPartId, Type = type });
                logger.LogDebug("复制页脚 Type={Type} -> {Id}", type, newPartId);
            }

            // 按 schema 顺序插入：footerReference 在 headerReference 之前
            foreach (var fr in newFooterRefs.AsEnumerable().Reverse())
                targetSectPr.PrependChild(fr);
            foreach (var hr in newHeaderRefs.AsEnumerable().Reverse())
                targetSectPr.PrependChild(hr);
        }

        // ============ Part 关系复制 ============
        public static void CopyPartRelationships(
            OpenXmlPart sourcePart,
            OpenXmlPart targetPart,
            ILogger logger)
        {
            string targetXml;
            using (var stream = targetPart.GetStream())
            using (var reader = new StreamReader(stream))
                targetXml = reader.ReadToEnd();

            var idRegex = new Regex(@"(?:r:embed|r:id|r:link)\s*=\s*""([^""]+)""",
                                    RegexOptions.Compiled);
            var referencedIds = idRegex.Matches(targetXml)
                                       .Select(m => m.Groups[1].Value)
                                       .Distinct()
                                       .ToList();

            if (referencedIds.Count == 0)
            {
                WriteXmlBack(targetPart, targetXml);
                return;
            }

            var copiedParts = new Dictionary<OpenXmlPart, string>();
            var replacedIds = new Dictionary<string, string>();
            var brokenIds = new List<string>();

            foreach (var oldId in referencedIds)
            {
                if (replacedIds.ContainsKey(oldId)) continue;

                OpenXmlPart? srcChildPart = null;
                try { srcChildPart = sourcePart.GetPartById(oldId); }
                catch (ArgumentOutOfRangeException) { }
                catch (KeyNotFoundException) { }

                string? newRelId = null;

                if (srcChildPart != null)
                {
                    if (copiedParts.TryGetValue(srcChildPart, out var cachedRelId))
                    {
                        newRelId = cachedRelId;
                    }
                    else
                    {
                        try
                        {
                            if (srcChildPart is ImagePart srcImagePart)
                            {
                                var newImagePart = targetPart.AddNewPart<ImagePart>(srcImagePart.ContentType);
                                newRelId = targetPart.GetIdOfPart(newImagePart);
                                using var s = srcImagePart.GetStream();
                                using var t = newImagePart.GetStream(FileMode.Create, FileAccess.Write);
                                s.CopyTo(t);
                            }
                            else
                            {
                                var newPart = targetPart.AddNewPart<OpenXmlPart>(srcChildPart.ContentType);
                                newRelId = targetPart.GetIdOfPart(newPart);
                                using var s = srcChildPart.GetStream();
                                using var t = newPart.GetStream(FileMode.Create, FileAccess.Write);
                                s.CopyTo(t);
                            }
                            copiedParts[srcChildPart] = newRelId;
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "复制 Part 失败: {Uri}", srcChildPart.Uri);
                            brokenIds.Add(oldId);
                            continue;
                        }
                    }
                }
                else
                {
                    var extRel = sourcePart.ExternalRelationships.FirstOrDefault(r => r.Id == oldId);
                    if (extRel != null)
                    {
                        var newExtRel = targetPart.AddExternalRelationship(
                            extRel.RelationshipType, extRel.Uri);
                        newRelId = newExtRel.Id;
                    }
                    else
                    {
                        logger.LogWarning("源 Part 找不到关系 ID {Id}，将清理引用", oldId);
                        brokenIds.Add(oldId);
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(newRelId) || newRelId == oldId) continue;

                targetXml = ReplaceRelId(targetXml, oldId, newRelId);
                replacedIds[oldId] = newRelId;
            }

            foreach (var brokenId in brokenIds)
                targetXml = RemoveBrokenReference(targetXml, brokenId);

            WriteXmlBack(targetPart, targetXml);
        }

        private static string RemoveBrokenReference(string xml, string brokenId)
        {
            XDocument doc;
            try { doc = XDocument.Parse(xml); }
            catch
            {
                xml = xml.Replace($" r:id=\"{brokenId}\"", "");
                xml = xml.Replace($" r:embed=\"{brokenId}\"", "");
                xml = xml.Replace($" r:link=\"{brokenId}\"", "");
                return xml;
            }

            XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
            XNamespace r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

            var hyperlinks = doc.Descendants(w + "hyperlink")
                                .Where(h => (string?)h.Attribute(r + "id") == brokenId)
                                .ToList();

            foreach (var h in hyperlinks)
            {
                var children = h.Nodes().ToList();
                h.ReplaceWith(children);
            }

            foreach (var attrName in new[] { "embed", "link", "id" })
            {
                var attrs = doc.Descendants()
                               .Attributes(r + attrName)
                               .Where(a => a.Value == brokenId)
                               .ToList();
                foreach (var a in attrs) a.Remove();
            }

            return doc.ToString(SaveOptions.DisableFormatting);
        }

        private static string ReplaceRelId(string xml, string oldId, string newId)
        {
            xml = xml.Replace($"r:embed=\"{oldId}\"", $"r:embed=\"{newId}\"");
            xml = xml.Replace($"r:link=\"{oldId}\"", $"r:link=\"{newId}\"");
            xml = xml.Replace($"r:id=\"{oldId}\"", $"r:id=\"{newId}\"");
            return xml;
        }

        private static void WriteXmlBack(OpenXmlPart targetPart, string xml)
        {
            using var stream = targetPart.GetStream(FileMode.Create, FileAccess.Write);
            using var writer = new StreamWriter(stream);
            writer.Write(xml);
        }
    }
}
