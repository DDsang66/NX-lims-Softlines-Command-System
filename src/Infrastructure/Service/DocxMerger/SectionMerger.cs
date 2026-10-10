using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Service.DocxMerger
{
    internal static class SectionMerger
    {
        public static void Append(
            WordprocessingDocument targetDoc,
            string sourcePath,
            DocxMergeOptions options,
            ILogger logger)
        {
            var targetMain = targetDoc.MainDocumentPart
                             ?? throw new InvalidOperationException("目标文档缺少 MainDocumentPart");
            var targetBody = targetMain.Document.Body
                             ?? throw new InvalidOperationException("目标文档缺少 Body");

            using var sourceDoc = WordprocessingDocument.Open(sourcePath, false);
            var sourceMain = sourceDoc.MainDocumentPart;
            if (sourceMain == null)
            {
                logger.LogWarning("子文档 {File} 无 MainDocumentPart，跳过", sourcePath);
                return;
            }

            var sourceBody = sourceMain.Document.Body
                             ?? throw new InvalidOperationException("子文档缺少 Body");

            // ★ 只取一次，缓存
            var sourceSectPr = sourceBody.Descendants<SectionProperties>().FirstOrDefault();

            // 1. 移动目标末尾 sectPr
            MoveTrailingSectionPropertiesIntoLastParagraph(targetBody);

            // 2. 计算有效内容末尾
            var sourceChildren = sourceBody.ChildElements.ToList();
            int lastValidIndex = ComputeLastValidIndex(sourceChildren);

            // 3. 追加内容
            for (int i = 0; i <= lastValidIndex; i++)
            {
                var element = sourceChildren[i];
                if (element is SectionProperties) continue;

                if (element is Paragraph p && p.ParagraphProperties?.SectionProperties != null)
                {
                    var newPara = (Paragraph)p.CloneNode(true);
                    newPara.ParagraphProperties!.RemoveAllChildren<SectionProperties>();
                    targetBody.AppendChild(newPara);
                    continue;
                }

                targetBody.AppendChild(element.CloneNode(true));
            }

            // 4. 创建新节（★ 传入缓存的 sourceSectPr）
            var newSectPr = BuildSectionProperties(sourceSectPr, options);

            // 5. 复制页眉页脚（★ 传入缓存的 sourceSectPr）
            if (options.CopyHeaderFooter)
            {
                RelationshipCopier.CopyHeaderFooterReferences(
                    sourceMain, targetMain, sourceSectPr, newSectPr, logger);
            }

            targetBody.AppendChild(newSectPr);
        }

        private static void MoveTrailingSectionPropertiesIntoLastParagraph(Body targetBody)
        {
            var currentSectPr = targetBody.Elements<SectionProperties>().LastOrDefault();
            if (currentSectPr == null) return;

            currentSectPr.Remove();

            var lastPara = targetBody.Elements<Paragraph>().LastOrDefault();
            if (lastPara != null)
            {
                lastPara.ParagraphProperties ??= new ParagraphProperties();
                lastPara.ParagraphProperties.SectionProperties = currentSectPr;
            }
            else
            {
                var p = new Paragraph
                {
                    ParagraphProperties = new ParagraphProperties
                    {
                        SectionProperties = currentSectPr
                    }
                };
                targetBody.AppendChild(p);
            }
        }

        private static int ComputeLastValidIndex(IList<DocumentFormat.OpenXml.OpenXmlElement> children)
        {
            int lastValidIndex = children.Count - 1;
            for (int i = children.Count - 1; i >= 0; i--)
            {
                var el = children[i];
                if (el is SectionProperties) continue;

                if (el is Paragraph p)
                {
                    var text = p.InnerText?.Trim() ?? "";
                    if (string.IsNullOrEmpty(text))
                    {
                        lastValidIndex = i - 1;
                        continue;
                    }
                }
                break;
            }
            return lastValidIndex;
        }

        private static SectionProperties BuildSectionProperties(
            SectionProperties? sourceSectPr,     // ← 改成传入，不再内部取
            DocxMergeOptions options)
        {
            var newSectPr = new SectionProperties();

            if (sourceSectPr != null)
            {
                var sectionType = sourceSectPr.Elements<SectionType>().FirstOrDefault();
                newSectPr.AppendChild(sectionType?.CloneNode(true)
                    ?? new SectionType { Val = MapSectionMark(options.DefaultSectionBreak) });

                var pgSz = sourceSectPr.Elements<PageSize>().FirstOrDefault();
                if (pgSz != null) newSectPr.AppendChild(pgSz.CloneNode(true));

                var pgMar = sourceSectPr.Elements<PageMargin>().FirstOrDefault();
                if (pgMar != null) newSectPr.AppendChild(pgMar.CloneNode(true));
            }
            else
            {
                newSectPr.AppendChild(new SectionType
                {
                    Val = MapSectionMark(options.DefaultSectionBreak)
                });
            }

            return newSectPr;
        }

        private static SectionMarkValues MapSectionMark(SectionBreakType type) => type switch
        {
            SectionBreakType.NextPage => SectionMarkValues.NextPage,
            SectionBreakType.Continuous => SectionMarkValues.Continuous,
            SectionBreakType.EvenPage => SectionMarkValues.EvenPage,
            SectionBreakType.OddPage => SectionMarkValues.OddPage,
            _ => SectionMarkValues.NextPage
        };
    }
}
