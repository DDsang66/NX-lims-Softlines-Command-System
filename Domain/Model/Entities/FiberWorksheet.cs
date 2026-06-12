using NX_lims_Softlines_Command_System.src.Application.Contract.DTOs;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities
{
    /// <summary>
    /// 纤维分析工作表 — 聚合根
    /// </summary>
    [Table("fiber_worksheet")]
    public class FiberWorksheet
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        [Column("report_number")]
        public string ReportNumber { get; set; } = null!;

        [MaxLength(20)]
        [Column("component_type")]
        public string? ComponentType { get; set; }

        [MaxLength(200)]
        [Column("test_method")]
        public string? TestMethod { get; set; }

        [MaxLength(100)]
        [Column("buyer")]
        public string? Buyer { get; set; }

        [MaxLength(20)]
        [Column("status")]
        public string Status { get; set; } = "Draft";

        [MaxLength(50)]
        [Column("technician")]
        public string? Technician { get; set; }

        [MaxLength(50)]
        [Column("reviewer")]
        public string? Reviewer { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<FiberWorksheetDetail> Details { get; set; } = new List<FiberWorksheetDetail>();
        public virtual FiberWorksheetResult? Result { get; set; }

        // EF Core 无参构造
        public FiberWorksheet() { }

        /// <summary>新建工作表</summary>
        public FiberWorksheet(string reportNumber, string? buyer)
        {
            ReportNumber = reportNumber;
            Buyer = buyer;
        }

        #region 行为方法

        /// <summary>
        /// 根据前端提交的分析数据重建明细和结果（覆盖旧数据）
        /// </summary>
        public void RebuildFromAnalysis(BuildAnalysisDto dto)
        {
            ComponentType = dto.ComponentType;
            TestMethod = dto.Method?.Count > 0 ? string.Join(",", dto.Method) : null;
            Buyer = dto.Buyer;
            UpdatedAt = DateTime.UtcNow;

            Details.Clear();
            int sectionIndex = 0;

            if (dto.ComponentType == "Multi" && dto.MultipleBuildAnalysis != null)
                BuildMultiFiberDetails(dto, ref sectionIndex);
            else if (dto.ComponentType == "Single" && dto.SingleBuildAnalysis != null)
                BuildSingleFiberDetails(dto, ref sectionIndex);

            Result = new FiberWorksheetResult
            {
                Id = Guid.NewGuid(),
                WorksheetId = Id,
                VerifyResult = dto.VerifyResult,
                FinalResult = dto.FinalResult,
                DurabilityLabel = dto.DurabilityLabel,
                OtherLabel = dto.OtherLabel,
                Comprehensive = dto.Comprehensive,
                RecommendedLabel = dto.RecommendedLabel?.Count > 0
                    ? string.Join(", ", dto.RecommendedLabel) : null,
                ResultRemark = dto.ResultRemark,
                LabelRemark = dto.LabelRemark,
                JudgmentLabelRemark = dto.JudgmentLabelRemark,
                LanguageLabelRemark = dto.LanguageLabelRemark
            };
        }

        /// <summary>
        /// 应用计算结果，更新明细百分比、Result 字段、Remark 和状态
        /// </summary>
        public void ApplyCalculation(FiberCalculationResultDto calcResult)
        {
            Result ??= new FiberWorksheetResult
            {
                Id = Guid.NewGuid(),
                WorksheetId = Id
            };

            Result.RecommendedLabel = calcResult.RecommendedLabel;
            Result.Comprehensive = calcResult.MainCategory;

            foreach (var calcItem in calcResult.Items)
                foreach (var detail in Details.Where(d => d.Composition == calcItem.Composition))
                    detail.CalculatedPercent = calcItem.CombinedPercentage;

            Result.ResultRemark = GenerateResultRemark(calcResult);
            Result.LabelRemark = GenerateLabelRemark(calcResult);
            Result.JudgmentLabelRemark = GenerateJudgmentRemark(calcResult);
            Result.LanguageLabelRemark = GenerateLanguageRemark();

            SetStatus("InProgress");
        }

        /// <summary>
        /// 状态转换
        /// </summary>
        public void SetStatus(string newStatus)
        {
            Status = newStatus;
            UpdatedAt = DateTime.UtcNow;
        }

        #endregion

        #region 明细构建

        private void BuildMultiFiberDetails(BuildAnalysisDto dto, ref int sectionIndex)
        {
            if (dto.MultipleBuildAnalysis.fiberSplittingList != null)
            {
                foreach (var splitList in dto.MultipleBuildAnalysis.fiberSplittingList)
                {
                    if (splitList.SplittingRows == null) continue;
                    foreach (var row in splitList.SplittingRows)
                    {
                        if (string.IsNullOrWhiteSpace(row.FiberName)) continue;
                        Details.Add(new FiberWorksheetDetail
                        {
                            Id = Guid.NewGuid(), WorksheetId = Id,
                            SectionIndex = sectionIndex,
                            Composition = row.FiberName,
                            Trial1 = (decimal?)row.GSMTrail1,
                            Trial2 = (decimal?)row.GSMTrail2
                        });
                    }
                    sectionIndex++;
                }
            }

            if (dto.MultipleBuildAnalysis.fiberDissolvedList != null)
            {
                foreach (var dissolved in dto.MultipleBuildAnalysis.fiberDissolvedList)
                {
                    if (dissolved.DissolvedRows == null) continue;
                    foreach (var row in dissolved.DissolvedRows)
                    {
                        if (string.IsNullOrWhiteSpace(row.FiberName)) continue;
                        Details.Add(new FiberWorksheetDetail
                        {
                            Id = Guid.NewGuid(), WorksheetId = Id,
                            SectionIndex = sectionIndex,
                            Composition = row.FiberName,
                            Trial1 = (decimal?)row.GSMTrail1,
                            Trial2 = (decimal?)row.GSMTrail2,
                            HeaderTrial1 = (decimal?)dissolved.OriginalGSMTrail1,
                            HeaderTrial2 = (decimal?)dissolved.OriginalGSMTrail2
                        });
                    }
                    sectionIndex++;
                }
            }
        }

        private void BuildSingleFiberDetails(BuildAnalysisDto dto, ref int sectionIndex)
        {
            if (dto.SingleBuildAnalysis.SingleFiberRows == null) return;
            foreach (var row in dto.SingleBuildAnalysis.SingleFiberRows)
            {
                if (string.IsNullOrWhiteSpace(row.FiberName)) continue;
                Details.Add(new FiberWorksheetDetail
                {
                    Id = Guid.NewGuid(), WorksheetId = Id,
                    SectionIndex = sectionIndex++,
                    Composition = row.FiberName,
                    Trial1 = (decimal?)row.GSMTrail1
                });
            }
        }

        #endregion

        #region Remark 生成

        private static string GenerateResultRemark(FiberCalculationResultDto calcResult)
        {
            if (calcResult.Items.Count == 0) return string.Empty;
            var remarks = calcResult.Items.Select(item =>
                $"{item.Composition}: Net Dry Content {item.NetDryContent:F1}%, " +
                $"Moisture Regain {item.MoistureRegain:F1}%, " +
                $"Combined {item.CombinedPercentage:F1}%");
            return string.Join("; ", remarks);
        }

        private static string GenerateLabelRemark(FiberCalculationResultDto calcResult)
            => string.IsNullOrEmpty(calcResult.RecommendedLabel)
                ? string.Empty : $"Label: {calcResult.RecommendedLabel}";

        private static string GenerateJudgmentRemark(FiberCalculationResultDto calcResult)
        {
            var total = calcResult.Items.Sum(i => i.CombinedPercentage);
            var tolerance = Math.Abs(100m - total);
            return tolerance <= 0.5m
                ? $"Total {total:F1}% — Within tolerance (±0.5%)"
                : $"Total {total:F1}% — Exceeds tolerance (±0.5%), review required";
        }

        private static string GenerateLanguageRemark()
            => "Fiber composition tested in accordance with relevant standards.";

        #endregion
    }
}
