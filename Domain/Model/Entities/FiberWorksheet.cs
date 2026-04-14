using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities
{
    /// <summary>
    /// 纤维分析工作表主表
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
        public string? ComponentType { get; set; }  // Multi, Single

        [MaxLength(200)]
        [Column("test_method")]
        public string? TestMethod { get; set; }

        [MaxLength(100)]
        [Column("buyer")]
        public string? Buyer { get; set; }

        [MaxLength(20)]
        [Column("status")]
        public string Status { get; set; } = "Draft";  // Draft, InProgress, Completed, Reviewed

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

        [Timestamp]
        [Column("row_version")]
        public byte[]? RowVersion { get; set; }

        // Navigation properties
        public virtual ICollection<FiberWorksheetDetail> Details { get; set; } = new List<FiberWorksheetDetail>();
        public virtual FiberWorksheetResult? Result { get; set; }
    }
}
