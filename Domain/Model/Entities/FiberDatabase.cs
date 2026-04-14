using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NX_lims_Softlines_Command_System.Domain.Model.Entities
{
    /// <summary>
    /// 纤维数据库 - 存储纤维名称、多标准公定回潮率、定性特征等信息
    /// </summary>
    [Table("fiber_database")]
    public class FiberDatabase
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        [Column("fiber_name_en")]
        public string FiberNameEn { get; set; } = null!;

        [MaxLength(100)]
        [Column("fiber_name_cn")]
        public string? FiberNameCn { get; set; }

        [MaxLength(50)]
        [Column("category")]
        public string? Category { get; set; }  // Natural, Synthetic, Regenerated

        // 各标准公定回潮率
        [Column("moisture_regain_iso", TypeName = "decimal(5,2)")]
        public decimal? MoistureRegainIso { get; set; }

        [Column("moisture_regain_aatcc", TypeName = "decimal(5,2)")]
        public decimal? MoistureRegainAatcc { get; set; }

        [Column("moisture_regain_can", TypeName = "decimal(5,2)")]
        public decimal? MoistureRegainCan { get; set; }

        [Column("moisture_regain_kor", TypeName = "decimal(5,2)")]
        public decimal? MoistureRegainKor { get; set; }

        [Column("moisture_regain_gb", TypeName = "decimal(5,2)")]
        public decimal? MoistureRegainGb { get; set; }

        [Column("moisture_regain_cns", TypeName = "decimal(5,2)")]
        public decimal? MoistureRegainCns { get; set; }

        [Column("moisture_regain_jis", TypeName = "decimal(5,2)")]
        public decimal? MoistureRegainJis { get; set; }

        // 定性特征描述
        [MaxLength(500)]
        [Column("qualitative_description")]
        public string? QualitativeDescription { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
