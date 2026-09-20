using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.EMMA;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NX_lims_Softlines_Command_System.src.Application.Interface;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Util;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using System.Runtime.CompilerServices;

namespace NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine.WordTemplateAdapter
{
    public class DataSheetFillingEngine : IScopedDependency
    {
        private readonly IFileStorageService _fileStorage;

        public DataSheetFillingEngine(IFileStorageService fileStorage)
        {
            _fileStorage = fileStorage;
        }

        /// <summary>
        /// 主方法，流程入口
        /// </summary>
        /// <param name="model">数据模型</param>
        public string FillDataSheet(DataSheetModel model)
        {
            var templatePath = model.TemplateUrl;
            string docxFileName = $"{model.ReportNumber}_{DateTime.Now:yyMMddHHmmss}_DStoWashing.docx";
            string targetDocxPath = _fileStorage.CopyTemplate(
                templatePath,
                Path.Combine("DocxModel", "SaveDocx"),
                docxFileName);

            using var doc = WordprocessingDocument.Open(targetDocxPath, true);
            var mainPart = doc.MainDocumentPart!;

            // 1. 构建书签映射字典
            var bookmarkValues = BuildBookmarkValues(model);

            // 2. 替换所有匹配的书签
            ReplaceBookmarks(mainPart, bookmarkValues);

            mainPart.Document.Save();

            return docxFileName;
        }

        /// <summary>
        /// 构建书签映射字典
        /// </summary>
        private Dictionary<string, string> BuildBookmarkValues(DataSheetModel model)
        {
            var bookmarkValues = new Dictionary<string, string>
            {
                ["ReportNumber"] = model.ReportNumber,
                ["TestMethod"] = model.TestMethod,
                ["TestCondition"] = model.TestCondition
            };

            // 添加 SampleMap 的映射
            if (model.SampleMap?.SampleMetaData != null)
            {
                foreach (var sample in model.SampleMap.SampleMetaData)
                {
                    bookmarkValues[sample.Key] = sample.Value;
                }
            }

            if (model.AfterWashMap?.AfterWashMetaData != null) 
            {
                foreach (var afterWash in model.AfterWashMap.AfterWashMetaData) 
                {
                    bookmarkValues[afterWash.Key] = afterWash.Value;
                }
            }

            return bookmarkValues;
        }

        /// <summary>
        /// 替换文档中的所有书签
        /// </summary>
        private void ReplaceBookmarks(MainDocumentPart mainPart, Dictionary<string, string> bookmarkValues)
        {
            var bookmarks = mainPart.Document.Body!.Descendants<BookmarkStart>()
                .Where(b => bookmarkValues.ContainsKey(b.Name!)).ToList();

            foreach (var bookmark in bookmarks)
            {
                var bookmarkEnd = mainPart.Document.Body.Descendants<BookmarkEnd>()
                    .FirstOrDefault(e => e.Id == bookmark.Id);

                if (bookmarkEnd == null) continue;

                var paragraph = bookmark.Ancestors<Paragraph>().FirstOrDefault();
                if (paragraph == null) continue;

                var runsBetween = paragraph.Elements<Run>()
                    .SkipWhile(r => !r.Elements<BookmarkStart>().Any(b => b.Id == bookmark.Id))
                    .TakeWhile(r => !r.Elements<BookmarkEnd>().Any(e => e.Id == bookmarkEnd.Id))
                    .ToList();

                var targetValue = bookmarkValues[bookmark.Name!];
                ReplaceBookmarkContent(bookmark, bookmarkEnd, runsBetween, targetValue);
            }
        }

        /// <summary>
        /// 替换单个书签的内容
        /// </summary>
        private void ReplaceBookmarkContent(BookmarkStart bookmark, BookmarkEnd bookmarkEnd,
            List<Run> runsBetween, string targetValue)
        {
            if (runsBetween.Any())
            {
                // 保留第一个Run的格式，替换其文本
                var firstRun = runsBetween.First();
                firstRun.RemoveAllChildren<Text>();
                firstRun.Append(new Text(targetValue) { Space = SpaceProcessingModeValues.Preserve });

                // 移除范围内多余的Run
                foreach (var run in runsBetween.Skip(1))
                {
                    run.Remove();
                }
            }
            else
            {
                // 若书签内无内容，在 BookmarkStart 后插入新 Run
                var newRun = new Run(new Text(targetValue) { Space = SpaceProcessingModeValues.Preserve });
                bookmark.Parent!.InsertAfter(newRun, bookmark);
            }
        }
    }
}
