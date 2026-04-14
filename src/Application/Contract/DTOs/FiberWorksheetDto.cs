using System;
using System.Collections.Generic;

namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs
{
    public class FiberWorksheetDto
    {
        public Guid Id { get; set; }
        public string ReportNumber { get; set; } = null!;
        public string? ComponentType { get; set; }
        public string? TestMethod { get; set; }
        public string? Buyer { get; set; }
        public string Status { get; set; } = "Draft";
        public string? Technician { get; set; }
        public string? Reviewer { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<FiberWorksheetDetailDto> Details { get; set; } = new();
        public FiberWorksheetResultDto? Result { get; set; }
    }

    public class FiberWorksheetDetailDto
    {
        public Guid Id { get; set; }
        public int SectionIndex { get; set; }
        public string? Composition { get; set; }
        public decimal? Trial1 { get; set; }
        public decimal? Trial2 { get; set; }
        public decimal? HeaderTrial1 { get; set; }
        public decimal? HeaderTrial2 { get; set; }
        public decimal? CalculatedPercent { get; set; }
    }

    public class FiberWorksheetResultDto
    {
        public Guid Id { get; set; }
        public string? VerifyResult { get; set; }
        public string? FinalResult { get; set; }
        public string? DurabilityLabel { get; set; }
        public string? OtherLabel { get; set; }
        public string? Comprehensive { get; set; }
        public string? RecommendedLabel { get; set; }
        public string? ResultRemark { get; set; }
        public string? LabelRemark { get; set; }
        public string? JudgmentLabelRemark { get; set; }
        public string? LanguageLabelRemark { get; set; }
    }

    public class FiberWorksheetCreateDto
    {
        public string ReportNumber { get; set; } = null!;
        public string? ComponentType { get; set; }
        public string? TestMethod { get; set; }
        public string? Buyer { get; set; }
        public string? Technician { get; set; }

        public List<FiberWorksheetDetailCreateDto> Details { get; set; } = new();
        public FiberWorksheetResultCreateDto? Result { get; set; }
    }

    public class FiberWorksheetDetailCreateDto
    {
        public int SectionIndex { get; set; }
        public string? Composition { get; set; }
        public decimal? Trial1 { get; set; }
        public decimal? Trial2 { get; set; }
        public decimal? HeaderTrial1 { get; set; }
        public decimal? HeaderTrial2 { get; set; }
        public decimal? CalculatedPercent { get; set; }
    }

    public class FiberWorksheetResultCreateDto
    {
        public string? VerifyResult { get; set; }
        public string? FinalResult { get; set; }
        public string? DurabilityLabel { get; set; }
        public string? OtherLabel { get; set; }
        public string? Comprehensive { get; set; }
        public string? RecommendedLabel { get; set; }
        public string? ResultRemark { get; set; }
        public string? LabelRemark { get; set; }
        public string? JudgmentLabelRemark { get; set; }
        public string? LanguageLabelRemark { get; set; }
    }
}
