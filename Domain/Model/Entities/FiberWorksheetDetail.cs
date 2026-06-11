using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities
{
    /// <summary>
    /// 纤维分析工作表明细表
    /// </summary>
    [Table("fiber_worksheet_detail")]
    public class FiberWorksheetDetail
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("worksheet_id")]
        public Guid WorksheetId { get; set; }

        [Column("section_index")]
        public int SectionIndex { get; set; }  // 溶解步骤序号

        [MaxLength(100)]
        [Column("composition")]
        public string? Composition { get; set; }  // 纤维成分

        [Column("trial1", TypeName = "decimal(10,4)")]
        public decimal? Trial1 { get; set; }

        [Column("trial2", TypeName = "decimal(10,4)")]
        public decimal? Trial2 { get; set; }

        [Column("header_trial1", TypeName = "decimal(10,4)")]
        public decimal? HeaderTrial1 { get; set; }

        [Column("header_trial2", TypeName = "decimal(10,4)")]
        public decimal? HeaderTrial2 { get; set; }

        [Column("calculated_percent", TypeName = "decimal(10,4)")]
        public decimal? CalculatedPercent { get; set; }

        // Navigation property
        [ForeignKey("WorksheetId")]
        public virtual FiberWorksheet? Worksheet { get; set; }
    }
}
