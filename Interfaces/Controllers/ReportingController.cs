using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using NX_lims_Softlines_Command_System.Application.DTO;
using NX_lims_Softlines_Command_System.src.Domain.Share;

namespace NX_lims_Softlines_Command_System.Interfaces.Controllers
{
    [ApiController]
    [Route("api/reporting")]
    public class ReportingController:ControllerBase
    {

        private readonly IWebHostEnvironment _env;

        public ReportingController(IWebHostEnvironment env) 
        {
            _env = env;
        }
        /// <summary>
        /// 触发验证报告的格式逻辑检查（示例接口，实际逻辑根据需求实现）
        /// </summary>
        /// <returns></returns>
        [HttpGet("report-auth")]
        public async Task<Result> ReportingAuthAsync(string reportNum, string group)
        {
            //var result = await _reportingAppService.ReportingAuthAsync(string repoNum,string group,string buyer);

            return Result.Ok();
        }


        /// <summary>
        /// 最小word修改示例
        /// </summary>
        /// <param name="dto"></param>
        /// <returns></returns>
        [HttpPost("report-create")]
        public async Task<Result> ReportingCreateAsync([FromBody] CreateReportDto dto)
        {
            string templatePath = Path.Combine(_env.WebRootPath, "ReportModel", "basic_report_model.docx");
            string outputDir = Path.Combine(_env.WebRootPath, "Reports", DateTime.Now.ToString("yyyyMM"));

            // 确保输出目录存在
            Directory.CreateDirectory(outputDir);

            // 生成新文件名
            string fileName = $"{dto.ReportNum}_{Guid.NewGuid():N}.docx";
            string outputPath = Path.Combine(outputDir, fileName);

            // 复制模板
            System.IO.File.Copy(templatePath, outputPath, true);

            // 替换书签内容
            using (var doc = WordprocessingDocument.Open(outputPath, true))
            {
                var mainPart = doc.MainDocumentPart;
                var bookmarks = mainPart.Document.Body.Descendants<BookmarkStart>()
                    .Where(b => b.Name == "Buyer" || b.Name == "FiberContent")
                    .ToList();

                foreach (var bookmark in bookmarks)
                {
                    // 找到对应的BookmarkEnd
                    var bookmarkEnd = mainPart.Document.Body.Descendants<BookmarkEnd>()
                        .FirstOrDefault(e => e.Id == bookmark.Id);

                    if (bookmarkEnd != null)
                    {
                        // 获取书签所在的段落
                        var paragraph = bookmark.Ancestors<Paragraph>().FirstOrDefault();
                        if (paragraph != null)
                        {
                            // 获取书签范围内的所有Run
                            var runsBetween = paragraph.Elements<Run>()
                                .SkipWhile(r => !r.Elements<BookmarkStart>().Any(b => b.Id == bookmark.Id))
                                .TakeWhile(r => !r.Elements<BookmarkEnd>().Any(e => e.Id == bookmarkEnd.Id))
                                .ToList();

                            if (runsBetween.Any())
                            {
                                // 替换第一个Run的内容
                                var firstRun = runsBetween.First();
                                firstRun.RemoveAllChildren<Text>();
                                firstRun.Append(new Text(bookmark.Name == "Buyer" ? dto.Buyer : dto.FiberContent));

                                // 移除其他Run
                                foreach (var run in runsBetween.Skip(1))
                                {
                                    run.Remove();
                                }
                            }
                            else
                            {
                                // 如果书签范围内没有Run，则在BookmarkStart后插入新Run
                                var newRun = new Run(new Text(bookmark.Name == "Buyer" ? dto.Buyer : dto.FiberContent));
                                bookmark.Parent.InsertAfter(newRun, bookmark);
                            }
                        }
                    }
                }
            }

            // 返回结果
            string relativePath = $"/Reports/{DateTime.Now:yyyyMM}/{fileName}";
            return Result.Ok();
        }
    }
}
