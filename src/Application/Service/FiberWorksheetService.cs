using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NX_lims_Softlines_Command_System.Domain.Model.Entities;
using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using NX_lims_Softlines_Command_System.src.Domain.Contract.Repository;
using NX_lims_Softlines_Command_System.src.Domain.Share;
using NX_lims_Softlines_Command_System.src.Domain.Share.DependencyInject;
using NX_lims_Softlines_Command_System.src.Infrastructure.TemplateEngine;

namespace NX_lims_Softlines_Command_System.src.Application.Service
{
    /// <summary>
    /// 纤维工作表服务
    /// </summary>
    public class FiberWorksheetService : IScopedDependency
    {
        private readonly IFiberWorksheetRepository _worksheetRepo;
        private readonly IFiberDatabaseRepository _fiberRepo;
        private readonly FiberCalculationService _calcService;
        private readonly WordTemplateEngine _templateEngine;
        private readonly IWebHostEnvironment _env;

        public FiberWorksheetService(
            IFiberWorksheetRepository worksheetRepo,
            IFiberDatabaseRepository fiberRepo,
            FiberCalculationService calcService,
            WordTemplateEngine templateEngine,
            IWebHostEnvironment env)
        {
            _worksheetRepo = worksheetRepo;
            _fiberRepo = fiberRepo;
            _calcService = calcService;
            _templateEngine = templateEngine;
            _env = env;
        }

        /// <summary>
        /// 构建成分分析报告 — 保存工作表数据到数据库
        /// </summary>
        public async Task<Result> BuildAnalysisAsync(BuildAnalysisDto dto)
        {
            // 1. 数据验证
            if (string.IsNullOrWhiteSpace(dto.ReportNumber))
                return Result.Fail("报告号不能为空", "VALIDATION_ERROR");

            // 2. 聚合根：新建或保留已有 Id/CreatedAt/Status
            var existing = await _worksheetRepo.GetByReportNumberAsync(dto.ReportNumber);
            var worksheet = new FiberWorksheet(dto.ReportNumber, dto.Buyer)
            {
                Id = existing?.Id ?? Guid.NewGuid(),
                Status = existing?.Status ?? "Draft",
                CreatedAt = existing?.CreatedAt ?? DateTime.UtcNow
            };

            worksheet.RebuildFromAnalysis(dto);

            // 3. 保存到数据库
            if (existing != null)
                await _worksheetRepo.UpdateAsync(worksheet);
            else
                await _worksheetRepo.AddAsync(worksheet);

            // 4. 生成Word文档
            try
            {
                GenerateWordDocument(worksheet);
            }
            catch (Exception ex)
            {
                // Word生成失败不阻塞数据保存，记录错误
                System.Diagnostics.Debug.WriteLine($"Word generation error: {ex.Message}");
            }

            return Result.Ok();
        }

        /// <summary>
        /// 基于工作表数据生成Word文档
        /// </summary>
        private void GenerateWordDocument(FiberWorksheet worksheet)
        {
            var templatePath = Path.Combine(_env.WebRootPath, "DocxModel", "FIBER_ANALYSIS_DATA_SHEET.docx");
            if (!File.Exists(templatePath)) return;

            var outputDir = Path.Combine(_env.WebRootPath, "DocxModel", "Output");
            Directory.CreateDirectory(outputDir);
            var outputPath = Path.Combine(outputDir, $"FIBER_ANALYSIS_{worksheet.ReportNumber}.docx");

            File.Copy(templatePath, outputPath, true);

            // 构建书签替换字典
            var bookmarkValues = new Dictionary<string, string>
            {
                { "ReportNumber", worksheet.ReportNumber ?? "" },
                { "Buyer", worksheet.Buyer ?? "" },
                { "TestMethod", worksheet.TestMethod ?? "" },
                { "ComponentType", worksheet.ComponentType ?? "" }
            };

            // 添加Result相关书签
            if (worksheet.Result != null)
            {
                bookmarkValues["VerifyResult"] = worksheet.Result.VerifyResult ?? "";
                bookmarkValues["FinalResult"] = worksheet.Result.FinalResult ?? "";
                bookmarkValues["RecommendedLabel"] = worksheet.Result.RecommendedLabel ?? "";
                bookmarkValues["ResultRemark"] = worksheet.Result.ResultRemark ?? "";
                bookmarkValues["LabelRemark"] = worksheet.Result.LabelRemark ?? "";
                bookmarkValues["JudgmentLabelRemark"] = worksheet.Result.JudgmentLabelRemark ?? "";
                bookmarkValues["LanguageLabelRemark"] = worksheet.Result.LanguageLabelRemark ?? "";
                bookmarkValues["DurabilityLabel"] = worksheet.Result.DurabilityLabel ?? "";
                bookmarkValues["OtherLabel"] = worksheet.Result.OtherLabel ?? "";
                bookmarkValues["Comprehensive"] = worksheet.Result.Comprehensive ?? "";
            }

            // 替换书签文本
            _templateEngine.ReplaceText(outputPath, bookmarkValues);

            // 填充成分数据表格
            using (var doc = WordprocessingDocument.Open(outputPath, true))
            {
                var table = _templateEngine.LocateTable(doc, "FiberDataTable");
                if (table == null)
                    table = _templateEngine.LocateTable(doc, "CompositionTable");

                if (table != null)
                {
                    // 为每个明细添加一行
                    foreach (var detail in worksheet.Details)
                    {
                        _templateEngine.AddRowToTable(table);
                        var lastRow = table.Elements<TableRow>().Last();
                        var cells = lastRow.Elements<TableCell>().ToList();

                        if (cells.Count >= 4)
                        {
                            FillCellText(cells[0], detail.Composition);
                            FillCellText(cells[1], detail.Trial1?.ToString("F4"));
                            FillCellText(cells[2], detail.Trial2?.ToString("F4"));
                            FillCellText(cells[3], detail.CalculatedPercent?.ToString("F2"));
                        }
                    }
                }

                doc.MainDocumentPart!.Document.Save();
            }
        }

        /// <summary>
        /// 填充单元格文本
        /// </summary>
        private static void FillCellText(TableCell cell, string? text)
        {
            var para = cell.Elements<Paragraph>().FirstOrDefault();
            if (para == null)
            {
                para = new Paragraph();
                cell.Append(para);
            }
            var run = para.Elements<Run>().FirstOrDefault();
            if (run == null)
            {
                run = new Run();
                para.Append(run);
            }
            run.RemoveAllChildren<Text>();
            run.Append(new Text(text ?? ""));
        }

        /// <summary>
        /// 执行成分计算并生成 Remark/Label 等结果
        /// </summary>
        public async Task<Result<FiberCalculationResultDto>> CalculateRemarkAsync(string reportNumber, string standard = "ISO")
        {
            if (string.IsNullOrWhiteSpace(reportNumber))
                return Result<FiberCalculationResultDto>.Fail("报告号不能为空", "VALIDATION_ERROR");

            // 1. 获取工作表
            var worksheet = await _worksheetRepo.GetByReportNumberAsync(reportNumber);
            if (worksheet == null)
                return Result<FiberCalculationResultDto>.Fail("工作表不存在", "NOT_FOUND");

            // 2. 将明细转为计算请求
            var requestItems = worksheet.Details
                .Where(d => !string.IsNullOrWhiteSpace(d.Composition))
                .GroupBy(d => d.Composition)
                .Select(g => new FiberCalculationItemDto
                {
                    Composition = g.Key!,
                    Trial1 = g.FirstOrDefault()?.Trial1,
                    Trial2 = g.FirstOrDefault()?.Trial2,
                    HeaderTrial1 = g.FirstOrDefault()?.HeaderTrial1,
                    HeaderTrial2 = g.FirstOrDefault()?.HeaderTrial2
                })
                .ToList();

            if (!requestItems.Any())
                return Result<FiberCalculationResultDto>.Fail("无有效的纤维成分数据", "NO_DATA");

            var request = new FiberCalculationRequestDto
            {
                Standard = standard,
                Items = requestItems
            };

            // 3. 执行计算，聚合根自行更新
            var calcResult = await _calcService.CalculateAsync(request);
            worksheet.ApplyCalculation(calcResult);

            // 4. 保存
            await _worksheetRepo.UpdateAsync(worksheet);

            return Result<FiberCalculationResultDto>.Ok(calcResult);
        }

        /// <summary>
        /// 获取工作表（含计算结果）
        /// </summary>
        public async Task<FiberWorksheetDto?> GetWorksheetAsync(string reportNumber)
        {
            var worksheet = await _worksheetRepo.GetByReportNumberAsync(reportNumber);
            if (worksheet == null) return null;

            return MapToDto(worksheet);
        }

        #region 映射

        private FiberWorksheetDto MapToDto(FiberWorksheet worksheet)
        {
            return new FiberWorksheetDto
            {
                Id = worksheet.Id,
                ReportNumber = worksheet.ReportNumber,
                ComponentType = worksheet.ComponentType,
                TestMethod = worksheet.TestMethod,
                Buyer = worksheet.Buyer,
                Status = worksheet.Status,
                Technician = worksheet.Technician,
                Reviewer = worksheet.Reviewer,
                CreatedAt = worksheet.CreatedAt,
                UpdatedAt = worksheet.UpdatedAt,
                Details = worksheet.Details.Select(d => new FiberWorksheetDetailDto
                {
                    Id = d.Id,
                    SectionIndex = d.SectionIndex,
                    Composition = d.Composition,
                    Trial1 = d.Trial1,
                    Trial2 = d.Trial2,
                    HeaderTrial1 = d.HeaderTrial1,
                    HeaderTrial2 = d.HeaderTrial2,
                    CalculatedPercent = d.CalculatedPercent
                }).ToList(),
                Result = worksheet.Result != null ? new FiberWorksheetResultDto
                {
                    Id = worksheet.Result.Id,
                    VerifyResult = worksheet.Result.VerifyResult,
                    FinalResult = worksheet.Result.FinalResult,
                    DurabilityLabel = worksheet.Result.DurabilityLabel,
                    OtherLabel = worksheet.Result.OtherLabel,
                    Comprehensive = worksheet.Result.Comprehensive,
                    RecommendedLabel = worksheet.Result.RecommendedLabel,
                    ResultRemark = worksheet.Result.ResultRemark,
                    LabelRemark = worksheet.Result.LabelRemark,
                    JudgmentLabelRemark = worksheet.Result.JudgmentLabelRemark,
                    LanguageLabelRemark = worksheet.Result.LanguageLabelRemark
                } : null
            };
        }

        #endregion
    }
}
