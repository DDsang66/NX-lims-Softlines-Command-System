using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;

namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs.AbrasionResistance_FW_
{
    /// <summary>
    /// 耐磨报告填充模型 — 对应 Word 模板中所有占位符
    /// </summary>
    public class AbrasionResistanceReportFillModel
    {
        // ==================== 报告头 ====================
        public string ReportNo { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string DateIn { get; set; } = string.Empty;
        public string DateOut { get; set; } = string.Empty;
        public string SampleRef { get; set; } = string.Empty;
        public string SampleDescription { get; set; } = string.Empty;
        public string AbrasionDistance { get; set; } = string.Empty;
        public string Remark { get; set; } = string.Empty;

        // ==================== 测试条件 ====================
        public string Condition { get; set; } = string.Empty;
        public string TestAtmosphere { get; set; } = string.Empty;
        public string CleanMethod { get; set; } = string.Empty;

        // ==================== 表头结果行 ====================
        public string SampleResult { get; set; } = string.Empty;
        public decimal? ResultDensity { get; set; }
        public decimal? ResultVolLoss { get; set; }
        public decimal? ResultARIndex { get; set; }
        public string Requirement { get; set; } = string.Empty;
        public string Conclusion { get; set; } = string.Empty;

        // ==================== 密度计算 (测试样品) ====================
        public string TestSpecimenA { get; set; } = string.Empty;
        public decimal? TestM1_A { get; set; }
        public decimal? TestM2_A { get; set; }
        public decimal? TestDensityA { get; set; }
        public string TestDensityA_Formula { get; set; } = string.Empty;  // ← 新增

        public string TestSpecimenB { get; set; } = string.Empty;
        public decimal? TestM1_B { get; set; }
        public decimal? TestM2_B { get; set; }
        public decimal? TestDensityB { get; set; }
        public string TestDensityB_Formula { get; set; } = string.Empty;  // ← 新增

        // ==================== 密度计算 (参照化合物) ====================
        public string RefSpecimenA { get; set; } = string.Empty;
        public decimal? RefM1_A { get; set; }
        public decimal? RefM2_A { get; set; }
        public decimal? RefDensityA { get; set; }
        public string RefDensityA_Formula { get; set; } = string.Empty;  // ← 新增

        public string RefSpecimenB { get; set; } = string.Empty;
        public decimal? RefM1_B { get; set; }
        public decimal? RefM2_B { get; set; }
        public decimal? RefDensityB { get; set; }
        public string RefDensityB_Formula { get; set; } = string.Empty;  // ← 新增

        // ==================== 体积损失 (每个specimen独立) ====================
        // Specimen 1
        public int Specimen1Number { get; set; } = 1;
        public decimal? Specimen1_BeforeWeight { get; set; }
        public decimal? Specimen1_AfterWeight { get; set; }
        public decimal? Specimen1_MassLoss { get; set; }
        public decimal? Specimen1_VolLoss { get; set; }
        public string Specimen1_VolLoss_Formula { get; set; } = string.Empty;  // ← 新增

        // Specimen 2
        public int Specimen2Number { get; set; } = 2;
        public decimal? Specimen2_BeforeWeight { get; set; }
        public decimal? Specimen2_AfterWeight { get; set; }
        public decimal? Specimen2_MassLoss { get; set; }
        public decimal? Specimen2_VolLoss { get; set; }
        public string Specimen2_VolLoss_Formula { get; set; } = string.Empty;  // ← 新增

        // Specimen 3
        public int Specimen3Number { get; set; } = 3;
        public decimal? Specimen3_BeforeWeight { get; set; }
        public decimal? Specimen3_AfterWeight { get; set; }
        public decimal? Specimen3_MassLoss { get; set; }
        public decimal? Specimen3_VolLoss { get; set; }
        public string Specimen3_VolLoss_Formula { get; set; } = string.Empty;  // ← 新增

        // ==================== 磨耗指数 (每个specimen独立) ====================
        public decimal? Specimen1ARIndex { get; set; }
        public string Specimen1_ARIndex_Formula { get; set; } = string.Empty;  // ← 新增

        public decimal? Specimen2ARIndex { get; set; }
        public string Specimen2_ARIndex_Formula { get; set; } = string.Empty;  // ← 新增

        public decimal? Specimen3ARIndex { get; set; }
        public string Specimen3_ARIndex_Formula { get; set; } = string.Empty;  // ← 新增


        // ==================== 底部 ====================
        public string GeneratedAt { get; set; } = string.Empty;
    }
}
