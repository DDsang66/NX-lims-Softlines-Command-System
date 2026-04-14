using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities
{
    /// <summary>
    /// 纤维分析工作表结果表
    /// </summary>
    [Table("fiber_worksheet_result")]
    public class FiberWorksheetResult
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("worksheet_id")]
        public Guid WorksheetId { get; set; }

        [MaxLength(20)]
        [Column("verify_result")]
        public string? VerifyResult { get; set; }  // Pass, Fail, Pending

        [MaxLength(20)]
        [Column("final_result")]
        public string? FinalResult { get; set; }  // Approved, Rejected, Review

        [MaxLength(200)]
        [Column("durability_label")]
        public string? DurabilityLabel { get; set; }

        [MaxLength(200)]
        [Column("other_label")]
        public string? OtherLabel { get; set; }

        [MaxLength(200)]
        [Column("comprehensive")]
        public string? Comprehensive { get; set; }

        [MaxLength(500)]
        [Column("recommended_label")]
        public string? RecommendedLabel { get; set; }

        [MaxLength(500)]
        [Column("result_remark")]
        public string? ResultRemark { get; set; }

        [MaxLength(500)]
        [Column("label_remark")]
        public string? LabelRemark { get; set; }

        [MaxLength(500)]
        [Column("judgment_label_remark")]
        public string? JudgmentLabelRemark { get; set; }

        [MaxLength(500)]
        [Column("language_label_remark")]
        public string? LanguageLabelRemark { get; set; }

        // Navigation property
        [ForeignKey("WorksheetId")]
        public virtual FiberWorksheet? Worksheet { get; set; }
    }
}
