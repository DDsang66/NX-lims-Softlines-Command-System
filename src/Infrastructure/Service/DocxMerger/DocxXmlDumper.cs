using DocumentFormat.OpenXml.Packaging;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.Service.DocxMerger
{
    internal static class DocxXmlDumper
    {
        public static void Dump(string docxPath, string? dumpDirectory, ILogger logger)
        {
            using var doc = WordprocessingDocument.Open(docxPath, false);
            var mainPart = doc.MainDocumentPart;
            if (mainPart == null) return;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("========== document.xml ==========");
            using (var stream = mainPart.GetStream())
            using (var reader = new StreamReader(stream))
                sb.AppendLine(reader.ReadToEnd());

            sb.AppendLine("========== document.xml.rels ==========");
            foreach (var pair in mainPart.Parts)
                sb.AppendLine($"Id={pair.RelationshipId}, Uri={pair.OpenXmlPart.Uri}");
            foreach (var rel in mainPart.ExternalRelationships)
                sb.AppendLine($"Id={rel.Id}, External, Uri={rel.Uri}");

            foreach (var hp in mainPart.HeaderParts)
            {
                sb.AppendLine($"--- HeaderPart {mainPart.GetIdOfPart(hp)} ---");
                using var s = hp.GetStream();
                using var r = new StreamReader(s);
                sb.AppendLine(r.ReadToEnd());
            }
            foreach (var fp in mainPart.FooterParts)
            {
                sb.AppendLine($"--- FooterPart {mainPart.GetIdOfPart(fp)} ---");
                using var s = fp.GetStream();
                using var r = new StreamReader(s);
                sb.AppendLine(r.ReadToEnd());
            }

            if (!string.IsNullOrEmpty(dumpDirectory))
            {
                Directory.CreateDirectory(dumpDirectory);
                var outFile = Path.Combine(dumpDirectory,
                    Path.GetFileNameWithoutExtension(docxPath) + ".dump.txt");
                File.WriteAllText(outFile, sb.ToString());
                logger.LogDebug("XML 诊断信息已写入 {File}", outFile);
            }
            else
            {
                logger.LogDebug("{Dump}", sb.ToString());
            }
        }
    }
}
