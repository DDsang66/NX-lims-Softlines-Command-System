using System;

namespace NX_lims_Softlines_Command_System.src.Application.Contract.DTOs
{
    public class FiberDatabaseDto
    {
        public Guid Id { get; set; }
        public string FiberNameEn { get; set; } = null!;
        public string? FiberNameCn { get; set; }
        public string? Category { get; set; }
        // 多标准公定回潮率
        public decimal? MoistureRegainIso { get; set; }
        public decimal? MoistureRegainAatcc { get; set; }
        public decimal? MoistureRegainCan { get; set; }
        public decimal? MoistureRegainKor { get; set; }
        public decimal? MoistureRegainGb { get; set; }
        public decimal? MoistureRegainCns { get; set; }
        public decimal? MoistureRegainJis { get; set; }
        // 定性特征
        public string? QualitativeDescription { get; set; }
        public bool IsActive { get; set; }
    }

    public class FiberDatabaseCreateDto
    {
        public string FiberNameEn { get; set; } = null!;
        public string? FiberNameCn { get; set; }
        public string? Category { get; set; }
        public decimal? MoistureRegainIso { get; set; }
        public decimal? MoistureRegainAatcc { get; set; }
        public decimal? MoistureRegainCan { get; set; }
        public decimal? MoistureRegainKor { get; set; }
        public decimal? MoistureRegainGb { get; set; }
        public decimal? MoistureRegainCns { get; set; }
        public decimal? MoistureRegainJis { get; set; }
        public string? QualitativeDescription { get; set; }
    }
}
